using MC_server.GameRoom.Models;
using System.Collections.Concurrent;

namespace MC_server.GameRoom.Services
{
    public class GameRoomService
    {
        // 각 게임 룸의 현재 세션 정보를 관리 -> 키는 룸 id, 값은 해당 룸의 세션 데이터
        private readonly ConcurrentDictionary<int, GameSession> _roomSessions = new ConcurrentDictionary<int, GameSession>();

        public void InitializeRooms()
        {
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
                SessionId = Guid.NewGuid().ToString(),
                RoomType = roomId,
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
