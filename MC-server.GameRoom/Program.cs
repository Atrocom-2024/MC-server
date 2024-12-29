using System.Net;
using System.Net.Sockets;

namespace MC_server.GameRoom
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await StartTcpServer();
        }

        private static async Task StartTcpServer()
        {
            var listener = new TcpListener(IPAddress.Any, 4000);
            listener.Start();
            Console.WriteLine("TCP server is listening on port 4000");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Client conncected!");
            }
        }
    }
}
