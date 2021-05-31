using System;

namespace ServerDemo2
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpServer server = TcpServer.Instance;
            server.Start();
            Console.ReadLine();
        }
    }
}
