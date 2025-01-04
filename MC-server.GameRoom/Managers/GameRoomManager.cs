using System.Collections.Concurrent;

using MC_server.GameRoom.Models;
using MC_server.Core.Models;
using MC_server.Core.Services;

namespace MC_server.GameRoom.Managers
{
    public class GameRoomManager
    {
        // 각 게임 룸의 현재 세션 정보를 관리 -> 키는 룸 id, 값은 해당 룸의 세션 데이터
        private readonly ConcurrentDictionary<int, GameSession> _roomSessions = new ConcurrentDictionary<int, GameSession>();

        private readonly RoomService _roomService;

        public GameRoomManager(RoomService roomService)
        {
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
        }

        public async Task InitializeRooms()
        {
            // DB에서 룸의 기본 정보를 모두 받아옴
            var allRooms = await _roomService.GetAllRoomsAsync();

            // 데이터 출력
            if (allRooms != null && allRooms.Count > 0)
            {
                foreach (var room in allRooms)
                {
                    _roomSessions[room.RoomId] = CreateNewSession(room);
                    Console.WriteLine($"Room ID: {room.RoomId}, TargetPayout: {room.TargetPayout}, BaseJackpotAmount: {room.BaseJackpotAmount}");
                }
            }
            Console.WriteLine("[socket] Initialized 10 game rooms");
        }

        public void ResetSession(Room room)
        {
            _roomSessions[room.RoomId] = CreateNewSession(room);
        }


        // 인스턴스를 메서드 내에서 참조하지 않기 때문에 정적 메서드로 선언
        public static GameSession CreateNewSession(Room room)
        {
            return new GameSession
            {
                TotalBetAmount = 0,
                TotalUser = 0,
                TotalJackpotAmount = 0,
                IsJackpot = false,
                TargetPayout = room.TargetPayout,
                MaxBetAmount = room.MaxBetAmount,
                MaxUser = room.MaxUser,
                BaseJackpotAmount = room.BaseJackpotAmount,
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
