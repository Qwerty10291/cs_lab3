using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

class Server
{
    private TcpListener server;
    private ConcurrentDictionary<int, string> fileIdToName = new ConcurrentDictionary<int, string>();
    private ConcurrentDictionary<string, int> fileNameToId = new ConcurrentDictionary<string, int>();
    private ConcurrentDictionary<string, Mutex> fileMutex = new ConcurrentDictionary<string, Mutex>();
    private int currentId = 1;
    private const string dataFolderPath = "./data/";
    private Regex filenameValidator = new Regex(".*[\\/].*");
    private const string idFilePath = "index.txt";


    public Server(string host, int port)
    {
        server = new TcpListener(IPAddress.Parse(host), port);
    }

    public async Task Start()
    {
        LoadIdsFromFile();
        server.Start();
        Console.WriteLine("Server started...");

        try
        {
            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                HandleClientAsync(client);
            }
        }
        finally
        {
            SaveIdsToFile();
            server.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream))
        using (StreamWriter writer = new StreamWriter(stream))
        {
            try
            {
                string request = await reader.ReadLineAsync();
                string[] tokens = request.Split(' ');
                if (tokens[0] == "exit") {
                    Environment.Exit(0);
                }

                string fileName = tokens[1];
                if (tokens[0] == "GET" || tokens[0] == "DELETE") {
                    fileName = GetFileName(tokens[1], tokens[2]);
                    if (!ValidateFileName(fileName)) {
                        await writer.WriteLineAsync("400 Bad filename");
                        return;
                    }
                }

                if (tokens[0] == "PUT")
                {
                    await SaveFile(fileName, stream, writer);
                }
                else if (tokens[0] == "GET")
                {
                    await SendFile(fileName, stream);
                }
                else if (tokens[0] == "DELETE")
                {
                    await DeleteFile(fileName, writer);
                }
                else
                {
                    await writer.WriteLineAsync("400 Bad Request");
                }
            }
            finally
            {
                await writer.FlushAsync();
                client.Close();
            }
        }
    }

    private async Task SaveFile(string fileName, NetworkStream reader, StreamWriter writer)
    {
        if (FileExists(fileName))
        {
            await writer.WriteLineAsync("403 ALREADY EXISTS");
            return;
        }

        AcquireFile(fileName);
        using (FileStream file = File.OpenWrite(Path.Join(dataFolderPath, fileName)))
        {
            byte[] buff = new byte[1024];
            int recieved = 0;
            while ((recieved = await ReadWithTimeout(reader, buff, 1000)) > 0) {
                await file.WriteAsync(buff[0..recieved]);
            }
        }
        ReleaseFile(fileName);

        int fileId = currentId++;
        fileIdToName[fileId] = fileName;
        fileNameToId[fileName] = fileId;

        await writer.WriteLineAsync($"200 {fileId}");
    }

    private async Task SendFile(string fileName, NetworkStream stream)
    {
        if (!FileExists(fileName))
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes("404 NOT FOUND\n"));
            return;
        }
        AcquireFile(fileName);
        await stream.WriteAsync(Encoding.ASCII.GetBytes("200 FOUND\n"));
        string filePath = Path.Combine(dataFolderPath, fileName);
        using (FileStream file = File.OpenRead(filePath)) {
            var buff = new byte[1024];
            int readed;
            while ((readed =  await file.ReadAsync(buff)) > 0) {
                await stream.WriteAsync(buff[0..readed]);
            }
        }
        ReleaseFile(fileName);
    }

    private async Task DeleteFile(string fileName, StreamWriter writer)
    {
        if (!FileExists(fileName))
        {
            await writer.WriteAsync("404 NOT FOUND\n");
            return;
        }
        await writer.WriteAsync("200 FOUND\n");
        AcquireFile(fileName);
        File.Delete(Path.Combine(dataFolderPath, fileName));
        ReleaseFile(fileName);
        fileIdToName.TryRemove(fileNameToId[fileName], out _);
        fileNameToId.TryRemove(fileName, out _);
        fileMutex.TryRemove(fileName, out _);
    }

    private void LoadIdsFromFile()
    {
        if (File.Exists(idFilePath))
        {
            string[] lines = File.ReadAllLines(idFilePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split(' ');
                if (parts.Length == 2 && int.TryParse(parts[0], out int id))
                {
                    string fileName = parts[1];
                    fileIdToName[id] = fileName;
                    fileNameToId[fileName] = id;
                    currentId = Math.Max(currentId, id);
                }
            }
        }
    }

    public void onExit(object? sender, EventArgs e) {
        SaveIdsToFile();
    }
    private void SaveIdsToFile()
    {
        using (StreamWriter writer = new StreamWriter(idFilePath))
        {
            foreach (var pair in fileIdToName)
            {
                writer.WriteLine($"{pair.Key} {pair.Value}");
            }
        }
    }
    private string GetFileName(string identifierType, string identifier)
    {
        
        try
        {
            if (identifierType == "BY_ID")
            {
                if (int.TryParse(identifier, out int fileId) && fileIdToName.ContainsKey(fileId))
                    return fileIdToName[fileId];
                return "";
            }
            else if (identifierType == "BY_NAME")
            {
                return identifier;
            }
            else
            {
                throw new Exception($"unknown indentifier type {identifierType}");
            }
        }
        finally
        {
            
        }

    }
    private bool FileExists(string name)
    {
        
        var exists = fileNameToId.ContainsKey(name);
        
        return exists;
    }
    private void AcquireFile(string name)
    {
        Mutex mutex;
        if (fileMutex.ContainsKey(name)) {
            mutex = fileMutex[name];
        } else {
            mutex = new Mutex();
            fileMutex[name] = mutex;
        }
        
        mutex.WaitOne();
    }

    private void ReleaseFile(string name)
    {
        var mutex = fileMutex[name];
        mutex.ReleaseMutex();
    }

    private bool ValidateFileName(string name) {
        return !filenameValidator.IsMatch(name);
    }

    private async Task<int> ReadWithTimeout(NetworkStream stream, byte[] buffer, int timeout) {
        Task<int> res =  ReadAync(stream, buffer);
        await Task.WhenAny(res, Task.Delay(timeout));
        if (!res.IsCompleted) {
            return -1;
        }
        return await res;
    }

    private async Task<int> ReadAync(NetworkStream stream, byte[] buffer) {
        return await stream.ReadAsync(buffer);
    }
}