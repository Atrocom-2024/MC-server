using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MC_server.GameRoom
{
    public class Program
    {
        private static readonly List<TcpClient> ConnectedClients = new List<TcpClient>();

        public static async Task Main(string[] args)
        {
            await StartTcpServer();
        }

        private static async Task StartTcpServer()
        {
            var listener = new TcpListener(IPAddress.Any, 4000);
            listener.Start();
            Console.WriteLine("[socket] TCP server is listening on port 4000");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("[socket] Client conncected!");

                lock (ConnectedClients)
                {
                    ConnectedClients.Add(client);
                    Console.WriteLine(ConnectedClients);
                }

                _ = HandleClientAsync(client);
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (var networkStream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    while (true)
                    {
                        int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                        
                        if (bytesRead == 0)
                        {
                            // 클라이언트가 연결을 끊음
                            Console.WriteLine("[socket] Client disconnected");
                            break;
                        }

                        // 메세지 처리
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"[socket] Received: {message}");

                        // 응답 메세지 보내기
                        byte[] response = Encoding.UTF8.GetBytes("[socket] Message received!");
                        await networkStream.WriteAsync(response, 0, response.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("[socket] Connection closed");
            }
        }
    }
}
