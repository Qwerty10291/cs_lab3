using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    public static void Start()
    {
        string serverIP = "127.0.0.1"; 
        int serverPort = 8888; 

        try
        {
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                clientSocket.Connect(IPAddress.Parse(serverIP), serverPort);
                NetworkStream stream = new NetworkStream(clientSocket);

         
                string action;

                string fileName;
                string request;
                string response;

                Console.WriteLine("Enter action: 1 - get a file, 2 - create file, 3 - delete a file");
                action = Console.ReadLine();

                switch (action)
                {
                    case "1":
                        Console.WriteLine("Enter file name:");
                        fileName = Console.ReadLine();
                        request = $"GET {fileName}";

                        SendRequest(stream, request);
                        response = ReceiveResponse(stream);

                        if (response.StartsWith("200"))
                        { 
                            Console.WriteLine(response);
                        }

                        else if (response.StartsWith("404"))
                        {
                            Console.WriteLine("The response says that the file was not found!");
                        }
                        break;

                    case "2":
                        Console.WriteLine("Enter file name:");
                        fileName = Console.ReadLine();

                        Console.WriteLine("Enter file content:");
                        string fileContent = Console.ReadLine();


                        request = $"PUT {fileName} {fileContent}";
                        SendRequest(stream, request);
                        response = ReceiveResponse(stream);

                        if (response.StartsWith("200")) 
                        { 
                            Console.WriteLine("The response says that file was created!");
                        }

                        else if (response.StartsWith("403")) 
                        {
                            Console.WriteLine("The response says that creating the file was forbidden!");
                        }
                        break;

                    case "3":
                        Console.WriteLine("Enter file name:");
                        fileName = Console.ReadLine();

                        request = $"DELETE {fileName}";
                        SendRequest(stream, request);
                        response = ReceiveResponse(stream);

                        if (response.StartsWith("200"))
                        {
                            Console.WriteLine("The response says that the file was successfully deleted!.");
                        }

                        else if (response.StartsWith("404"))
                        {
                            Console.WriteLine("The response says that the file was not found!");
                        }
                        break;
                    case "exit":
                        request = "exit";
                        SendRequest(stream, request);
                        break;
                }

                
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
    }

    static string ReceiveResponse(NetworkStream stream)
    {
        byte[] responseData = new byte[256];
        int bytesRead = stream.Read(responseData, 0, responseData.Length);
        return Encoding.UTF8.GetString(responseData, 0, bytesRead);
    }
}
