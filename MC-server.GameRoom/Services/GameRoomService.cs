using System.Collections.Concurrent;
using MC_server.Core.Services;
using MC_server.GameRoom.Models;

namespace MC_server.GameRoom.Services
{
    public class GameRoomService
    {
        // 각 게임 룸의 현재 세션 정보를 관리 -> 키는 룸 id, 값은 해당 룸의 세션 데이터
        private readonly ConcurrentDictionary<int, GameSession> _roomSessions = new ConcurrentDictionary<int, GameSession>();

        private readonly RoomService _roomService;

        public GameRoomService(RoomService roomService)
        {
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
        }

        //public async void InitializeRooms()
        public void InitializeRooms()
        {
            //Console.WriteLine($"DB_DATABASE: {Environment.GetEnvironmentVariable("DB_DATABASE")}");
            //Console.WriteLine($"DB_HOST: {Environment.GetEnvironmentVariable("DB_HOST")}");
            //Console.WriteLine($"DB_PORT: {Environment.GetEnvironmentVariable("DB_PORT")}");
            //Console.WriteLine($"DB_USERNAME: {Environment.GetEnvironmentVariable("DB_USERNAME")}");
            //Console.WriteLine($"DB_PASSWORD: {Environment.GetEnvironmentVariable("DB_PASSWORD")}");

            //var allRooms = await _roomService.GetRoomByIdAsync(1);
            //Console.WriteLine(allRooms);

            //// 데이터 출력
            //if (allRooms != null && allRooms.Count > 0)
            //{
            //    Console.WriteLine("Rooms fetched successfully:");
            //    foreach (var room in allRooms)
            //    {
            //        Console.WriteLine($"Room ID: {room.RoomId}, Room Name: {room.TargetPayout}");
            //    }
            //}

            for (int roomId = 1; roomId <= 10; roomId++)
            {
                _roomSessions[roomId] = CreateNewSession(roomId);
            }

            Console.WriteLine("[socket] Initialized 10 game rooms");
        }

        public void ResetSession(int roomId)
        {
            _roomSessions[roomId] = CreateNewSession(roomId);
        }

        public GameSession CreateNewSession(int roomId)
        {
            return new GameSession
            {
                TotalBetAmount = 0,
                TotalUser = 0,
                TotalJackpotAmount = 0,
                IsJackpot = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        public GameSession? GetSession(int roomId)
        {
            _roomSessions.TryGetValue(roomId, out var session);
            return session;
        }

        public ConcurrentDictionary<int, GameSession> GetAllSessions()
        {
            return _roomSessions;
        }
    }
}
