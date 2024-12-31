using System.Collections.Concurrent;
using ProtoBuf;

using MC_server.GameRoom.Models;

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
                _roomSessions[roomId] = new GameSession()
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

            Console.WriteLine("[socket] Initialized 10 game rooms");
        }

        public void ResetSession(int roomId)
        {
            _roomSessions[roomId] = new GameSession
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

        // Protobuf로 직렬화된 GameSession 반환
        public byte[] SerializeGameSession(int roomId)
        {
            if (_roomSessions.TryGetValue(roomId, out var session))
            {
                using var memoryStream = new MemoryStream();
                Serializer.Serialize(memoryStream, session);
                return memoryStream.ToArray();
            }

            throw new InvalidOperationException($"Room {roomId} does not exist.");
        }

        // Protobuf로 직렬화된 데이터를 GameSession으로 복원
        public GameSession DeserializeGameSession(byte[] data)
        {
            using var memoryStream = new MemoryStream(data);
            return Serializer.Deserialize<GameSession>(memoryStream);
        }
    }
}
