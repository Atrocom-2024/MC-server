using MC_server.GameRoom.Managers;
using MC_server.Core.Services;

namespace MC_server.GameRoom.Schedulers
{
    public class SessionScheduler
    {
        private readonly GameRoomManager _gameRoomManager;

        private readonly RoomService _roomService;

        public SessionScheduler(GameRoomManager gameRoomManager, RoomService roomService)
        {
            _gameRoomManager = gameRoomManager;
            _roomService = roomService;
        }

        // 비동기 메서드에서 반환값이 없을 땐 Task를 사용
        public async Task StartSessionTimers()
        {
            var allRooms = await _roomService.GetAllRoomsAsync();

            if (allRooms != null && allRooms.Count > 0)
            {
                foreach (var room in allRooms)
                {
                    Timer timer = new Timer(_ =>
                    {
                        _gameRoomManager.ResetSession(room);
                        Console.WriteLine($"[socket] Room {room.RoomId}: New session started.");
                    }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
                }
            }
        }
    }
}
