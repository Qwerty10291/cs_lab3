class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "server")
        {
            Client.Start();
        }
        else
        {
            var server = new Server("127.0.0.1", 8081);
            AppDomain.CurrentDomain.ProcessExit += server.onExit;
            server.Start().Wait();
        }
    }
}
