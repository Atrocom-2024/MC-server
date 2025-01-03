using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

using MC_server.GameRoom.Extensions;
using MC_server.GameRoom.Handlers;
using MC_server.GameRoom.Managers;
using MC_server.GameRoom.Services;
using DotNetEnv;

namespace MC_server.GameRoom
{
    // 의존성 주입(DI) 사용
    public class Program
    {
        // 의존성 필드 선언
        private readonly GameRoomService _gameRoomService;
        private readonly SessionService _sessionService;
        private readonly ClientManager _clientManager;
        private readonly ClientHandler _clientHandler;

        // 의존성 주입 생성자
        public Program(GameRoomService gameRoomService, SessionService sessionService, ClientManager clientManager, ClientHandler clientHandler)
        {
            _gameRoomService = gameRoomService ?? throw new ArgumentNullException(nameof(gameRoomService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _clientHandler = clientHandler ?? throw new ArgumentNullException(nameof(clientHandler));
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            Env.Load(Path.Combine(AppContext.BaseDirectory, ".env"));

            // 1. 서비스 구성
            var serviceProvider = ServiceConfigurator.ConfigureServices();

            // 2. Program 인스천스 생성 및 실행
            var program = serviceProvider.GetRequiredService<Program>();
            await program.Run();
        }

        public async Task Run()
        {
            // 1. 게임 룸 서비스 초기화
            _gameRoomService.InitializeRooms();// 초기화: 10개의 룸 생성

            // 2. 세션 타이머 시작
            _sessionService.StartSessionTimers();

            // 3. TCP 서버 시작
            await StartTcpServer();
        }

        private async Task StartTcpServer()
        {
            // 1. TCP 리스너 초기화
            var listener = new TcpListener(IPAddress.Any, 4000);
            listener.Start();
            Console.WriteLine("[socket] TCP server is listening on port 4000");

            while (true)
            {
                try
                {
                    // 3. 클라이언트 연결 대기
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("[socket] Client conncected!");

                    // 4. 연결된 클라이언트를 ClientManager에 추가
                    _clientManager.AddClient(client);

                    // 5. 클라이언트 처리 시작
                    _ = _clientHandler.HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[socket] Error accepting client: {ex.Message}");
                }
            }
        }
    }
}
