using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MC_server.GameRoom
{
    public class Program
    {
        private static readonly List<TcpClient> ConnectedClients = new List<TcpClient>();
        private static GameSession? CurrentSession;
        private static readonly object SessionLock = new object();

        public static async Task Main(string[] args)
        {
            // 초기 게임 세션 생성
            CurrentSession = new GameSession()
            {
                GameId = Guid.NewGuid().ToString(),
                RoomType = null,
                TotalBetAmount = 0,
                TotalUser = 0,
                TotalJackpotAmount = 0,
                IsJackpot = false,
                CreatedAt = DateTime.UtcNow
            };

            // 세션 타이머 시작
            StartSessionTimer();

            // TCP 서버 시작
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
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"[socket] Received: {receivedMessage}");

                        // 응답 메세지 보내기
                        byte[] response = Encoding.UTF8.GetBytes("[socket] Message received!");

                        // JSON 메세지 처리
                        var request = JsonSerializer.Deserialize<BetRequest>(receivedMessage);
                        if (request != null)
                        {
                            // 배팅 처리 및 세션 데이터 업데이트
                            var updatedSession = HandleBetting(request);

                            if (updatedSession != null)
                            {
                                // 변경된 세션 데이터 브로드캐스트
                                BroadcastMessage(updatedSession);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error: {ex.Message}");
            }
            finally
            {
                lock (ConnectedClients)
                {
                    ConnectedClients.Remove(client);
                }

                client.Close();
                Console.WriteLine("[socket] Connection closed");
            }
        }

        private static void StartSessionTimer()
        {
            Timer timer = new Timer(_ =>
            {
                lock (SessionLock)
                {
                    // 새로운 세션 생성
                    CurrentSession = new GameSession()
                    {
                        GameId = Guid.NewGuid().ToString(),
                        RoomType = null,
                        TotalBetAmount = 0,
                        TotalUser = 0,
                        TotalJackpotAmount = 0,
                        IsJackpot = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    Console.WriteLine($"[socket] New session {CurrentSession.GameId} started.");

                    // 새로운 세션 정보 브로드캐스트
                    BroadcastMessage(CurrentSession);
                }
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private static GameSession? HandleBetting(BetRequest request)
        {
            lock (SessionLock)
            {
                // 배팅 로직 처리
                if (CurrentSession != null)
                {
                    CurrentSession.TotalBetAmount += request.BetAmount;
                    CurrentSession.TotalJackpotAmount += (long)Math.Round(request.BetAmount * 0.1); // 배팅 금액의 10%가 잭팟으로 누적

                    Console.WriteLine($"[socket] Session {CurrentSession.GameId} updated: TotalBet = {CurrentSession.TotalBetAmount}, Jackpot = {CurrentSession.TotalJackpotAmount}");
                    return CurrentSession;
                }
                return null;
            }
        }

        private static void BroadcastMessage(object message)
        {
            string jsonMessage = JsonSerializer.Serialize(message);

            lock (ConnectedClients)
            {
                foreach (var client in ConnectedClients)
                {
                    try
                    {
                        if (client.Connected)
                        {
                            var stream = client.GetStream();
                            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
                            stream.WriteAsync(data, 0, data.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[socket] Failed to send message to client: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"[socket] Broadcasted: {jsonMessage}");
        }
    }

    public class BetRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int BetAmount { get; set; }
    }

    public class GameSession
    {
        public string GameId { get; set; } = string.Empty;
        public int? RoomType { get; set; }
        public long TotalBetAmount { get; set; }
        public int TotalUser { get; set; }
        public long TotalJackpotAmount { get; set; }
        public bool IsJackpot { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
