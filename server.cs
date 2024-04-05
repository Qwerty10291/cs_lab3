using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
class Server
{
    public static void Start()
    {
        int port = 8888;

        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

        listener.Bind(localEndPoint);
        listener.Listen(10);

        Console.WriteLine("Server started!");

        while (true)
        {
            Socket handler = listener.Accept();
            byte[] data = new byte[1024];
            int bytes = handler.Receive(data);
            var iterator = data.AsEnumerable();
            string method = Encoding.ASCII.GetString(iterator.TakeWhile(x => x != ' ' && x != 0).ToArray());
            iterator = iterator.Skip(method.Length + 1);
            string path = Encoding.ASCII.GetString(iterator.TakeWhile(x => x != ' ' && x != 0).ToArray());
            iterator = iterator.Skip(path.Length + 1);
            string response = "";
            switch (method)
            {  
                case "exit":
                    sendMessage(handler, "Server stopped!");
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    listener.Close();
                    return;
                case "PUT":
                    try
                    {
                        FileStream file = File.OpenWrite(path);
                        file.Write(data[(path.Length + method.Length + 2)..bytes]);
                        if (handler.Poll(1000000, SelectMode.SelectRead)) {
                            bytes = handler.Receive(data);
                            while (bytes > 0) {
                                file.Write(data[0..bytes]);
                            }
                        }
                        file.Close();
                    }
                    catch (Exception)
                    {
                        response = "403";
                    }
                    break;
                case "GET":
                    if (File.Exists(path))
                    {   
                        sendMessage(handler, "200 " + File.ReadAllText(path));
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        continue;
                    }
                    else
                    {
                        response = "404";
                    }
                    break;
                case "DELETE":
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        response = "200";
                    }

                    else
                    {
                        response = "404";
                    }
                    break;
                default:
                    response = "400";
                    break;
            }
            sendMessage(handler, response);
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }

    private static void sendMessage(Socket sock, string msg) {
        sock.Send(Encoding.UTF8.GetBytes(msg));
    }
}
