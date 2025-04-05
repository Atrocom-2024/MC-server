using System.Net;
using System.Net.Sockets;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;

using MC_server.GameRoom.Extensions;
using MC_server.GameRoom.Handlers;
using MC_server.GameRoom.Managers;

namespace MC_server.GameRoom
{
    // 의존성 주입(DI) 사용
    public class Program
    {
        // 의존성 필드 선언
        private readonly GameRoomManager _gameRoomManager;
        private readonly GameRoomHandler _gameRoomHandler;

        // 의존성 주입 생성자
        public Program(GameRoomManager gameRoomManager, GameRoomHandler gameRoomHandler)
        {
            _gameRoomManager = gameRoomManager ?? throw new ArgumentNullException(nameof(gameRoomManager));
            _gameRoomHandler = gameRoomHandler ?? throw new ArgumentNullException(nameof(gameRoomHandler));
        }

        public static async Task Main(string[] args)
        {
            // 실행 파일의 경로를 기반으로 .env 파일 로드
            var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            Env.Load(envPath);

            // 1. 서비스 구성
            var serviceProvider = ServiceConfigurator.ConfigureServices();

            // 2. Program 인스턴스 생성 및 실행
            var program = serviceProvider.GetRequiredService<Program>();
            await program.Run();
        }

        public async Task Run()
        {
            // 1. 게임 룸 세션 초기화
            await _gameRoomManager.InitializeRooms();// 초기화: 10개의 룸 생성

            // 2. TCP 서버 시작
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
                    // 2. 클라이언트 연결 대기
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    //Console.WriteLine("[socket] Client conncected!");

                    // 3. 게임 룸 처리 시작
                    _ = _gameRoomHandler.HandleGameRoomAsync(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[socket] Error accepting client: {ex.Message}");
                }
            }
        }
    }
}
