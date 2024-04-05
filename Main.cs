
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
            Server.Start();
        }
    }

}
