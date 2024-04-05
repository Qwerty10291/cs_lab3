using System.Net.Sockets;
using System.Text;

class Client
{
    public static void Start()
    {
        string serverIP = "127.0.0.1";
        int serverPort = 8081;

        try
        {
            using (TcpClient client = new TcpClient(serverIP, serverPort))
            using (NetworkStream stream = client.GetStream())
            {
                string method;
                string identifierType;
                string identifier;
                string request = "";
                string response;

                Console.WriteLine("enter request method(GET, PUT, DELETE, exit):");
                method = Console.ReadLine();
                if (method == "exit")
                {
                    SendRequest(stream, "exit r\n");
                    return;
                }
                switch (method)
                {
                    case "PUT":
                        Console.WriteLine("enter file name:");
                        identifier = Console.ReadLine();
                        if (!File.Exists(identifier))
                        {
                            Console.WriteLine("this file does not exist");
                            return;
                        }
                        SendRequest(stream, $"PUT {identifier}\n");
                        using (FileStream file = File.OpenRead(identifier))
                        {
                            var buff = new byte[1024];
                            int readed;
                            while ((readed = file.Read(buff)) > 0)
                            {
                                stream.Write(buff[0..readed]);
                            }
                        }
                        stream.Flush();
                        break;
                    case "GET":
                        Console.WriteLine("enter identifier type(id, name):");
                        identifierType = Console.ReadLine();
                        Console.WriteLine("enter identifier:");
                        identifier = Console.ReadLine();
                        SendRequest(stream, $"GET {(identifierType == "id" ? "BY_ID" : "BY_NAME")} {identifier}\n");
                        RecieveFile(stream, identifier);
                        return;
                    case "DELETE":
                        Console.WriteLine("enter identifier type(id, name):");
                        identifierType = Console.ReadLine();
                        Console.WriteLine("enter identifier:");
                        identifier = Console.ReadLine();
                        SendRequest(stream, $"DELETE {(identifierType == "id" ? "BY_ID" : "BY_NAME")} {identifier}\n");
                        break;
                }
                ReceiveResponse(stream);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    static void SendRequest(NetworkStream stream, string request)
    {
        byte[] requestData = Encoding.UTF8.GetBytes(request);
        stream.Write(requestData, 0, requestData.Length);
        stream.Flush();
    }

    static void ReceiveResponse(NetworkStream stream)
    {
        byte[] responseData = new byte[256];
        int bytesRead = 0;
        using (Stream stdout = Console.OpenStandardOutput())
        {
            while ((bytesRead = stream.Read(responseData, 0, responseData.Length)) > 0)
            {
                stdout.Write(responseData, 0, bytesRead);
            }
        }
    }

    static void RecieveFile(NetworkStream stream, string identifier) {
        byte[] header = new byte[3];
        while (stream.Read(header) == 0);
        var status = Encoding.ASCII.GetString(header);
        if (status == "200") {
            using (FileStream file = File.OpenWrite(identifier))
            {   
                var buff = new byte[1024];
                int readed;
                while (stream.CanRead && (readed = stream.Read(buff)) > 0)
                {
                    file.Write(buff[0..readed]);
                }
            }
            Console.WriteLine("file created");
        } else {
            Console.WriteLine(string.Join(' ', status));
        }
    }
}
