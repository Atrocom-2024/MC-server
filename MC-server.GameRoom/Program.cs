using System.Net;
using System.Net.Sockets;

using MC_server.GameRoom.Handlers;
using MC_server.GameRoom.Managers;
using MC_server.GameRoom.Services;

namespace MC_server.GameRoom
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // 1. 게임 룸 서비스 초기화
            GameRoomService gameRoomService = new GameRoomService();
            gameRoomService.InitializeRooms();// 초기화: 10개의 룸 생성

            // 2. 세션 타이머 시작
            SessionService sessionService = new SessionService(gameRoomService);
            sessionService.StartSessionTimers();

            // 3. 클라이언트 매니저 초기화
            ClientManager clientManager = new ClientManager();

            // 4. TCP 서버 시작
            await StartTcpServer(gameRoomService, clientManager);
        }

        private static async Task StartTcpServer(GameRoomService gameRoomService, ClientManager clientManager)
        {
            // 1. TCP 리스너 초기화
            var listener = new TcpListener(IPAddress.Any, 4000);
            listener.Start();
            Console.WriteLine("[socket] TCP server is listening on port 4000");

            // 2. ClientHandler 인스턴스 생성
            var clientHandler = new ClientHandler(gameRoomService, clientManager);

            while (true)
            {
                try
                {
                    // 3. 클라이언트 연결 대기
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("[socket] Client conncected!");

                    // 4. 연결된 클라이언트를 ClientManager에 추가
                    clientManager.AddClient(client);

                    // 5. 클라이언트 처리 시작
                    _ = clientHandler.HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[socket] Error accepting client: {ex.Message}");
                }

            }
        }
    }
}
