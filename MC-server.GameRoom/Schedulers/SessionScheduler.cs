using System.Collections.Concurrent;

using MC_server.GameRoom.Managers;
using MC_server.Core.Services;

namespace MC_server.GameRoom.Schedulers
{
    public class SessionScheduler
    {
        private readonly GameRoomManager _gameRoomManager;
        private readonly RoomService _roomService;

        // 룸별 타이머 관리
        private readonly ConcurrentDictionary<int, Timer> _roomTimers = new ConcurrentDictionary<int, Timer>();

        public SessionScheduler(GameRoomManager gameRoomManager, RoomService roomService)
        {
            _gameRoomManager = gameRoomManager;
            _roomService = roomService;
        }

        // 비동기 메서드에서 반환값이 없을 땐 Task를 사용
        // TODO: 세션이 끝날 때 유저들에게 payout 반환
        public async Task StartSessionTimers()
        {
            var allRooms = await _roomService.GetAllRoomsAsync();

            if (allRooms != null && allRooms.Count > 0)
            {
                foreach (var room in allRooms)
                {
                    // TODO: 모든 룸이 같은 시간에 초기화되는 것이 아님, 어느 한 룸에서 잭팟이 터지면 초기화되면서 초기화 시간이 변경될 수 있음
                    // 따라서 룸 별로 다르게 시간을 재야 함
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
