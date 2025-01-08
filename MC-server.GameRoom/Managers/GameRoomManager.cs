using System.Collections.Concurrent;

using MC_server.GameRoom.Managers.Models;
using MC_server.GameRoom.Utils;
using MC_server.Core.Models;
using MC_server.Core.Services;

namespace MC_server.GameRoom.Managers
{
    public class GameRoomManager
    {
        // 각 게임 룸의 현재 세션 정보를 관리 -> 키는 룸 id, 값은 해당 룸의 세션 데이터
        private readonly ConcurrentDictionary<int, GameSession> _roomSessions = new ConcurrentDictionary<int, GameSession>();
        // 각 룸별 타이머 관리
        private readonly ConcurrentDictionary<int, Timer> _roomTimers = new ConcurrentDictionary<int, Timer>();

        private readonly RoomService _roomService;

        private readonly object _lock = new object();

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
                    // 게임 세션 초기화
                    _roomSessions[room.RoomId] = GameSessionUtils.CreateNewSession(room);

                    // 타이머 초기화
                    StartRoomTimer(room);
                }
            }
            Console.WriteLine("[socket] Initialized 10 game rooms");
        }

        // 특정 룸의 타이머 시작
        public void StartRoomTimer(Room room)
        {
            if (_roomTimers.ContainsKey(room.RoomId))
            {
                Console.WriteLine($"[socket] Timer for Room {room.RoomId} is already running.");
                return;
            }

            Timer timer = new Timer(_ =>
            {
                // 잭팟이 터지거나 시간이 만료되면 초기화
                _ = ResetRoomSession(room.RoomId);
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _roomTimers[room.RoomId] = timer;
            Console.WriteLine($"[socket] Timer started for Room {room.RoomId}");
        }

        // 특정 룸의 타이머 중단
        public void StopRoomTimer(int roomId)
        {
            if (_roomTimers.TryRemove(roomId, out var timer))
            {
                timer.Dispose();
            }
            else
            {
                Console.WriteLine($"[socket] No active timer found for Room {roomId}");
            }
        }

        // 특정 룸 초기화
        private async Task ResetRoomSession(int roomId)
        {
            // TODO: 룸 초기화 시 해당 룸에 접속 중인 ClientManager의 _clientStates도 초기화가 되어야 함.
            // TODO: 룸 초기화 시 현재 접속중인 유저를 유지시키면서 세션을 재생성해야함
            // TODO: 룸 초기화 시 IsJackpot이 false이면 기존의 잭팟 금액 유지
            // TODO: 잭팟이 터졌을 때 해당 룸 세션 초기화 기능 -> 다른 유저들에겐 TotalBetAmount의 10% 반환 후 페이아웃은 반환되지 않고 초기화
            Console.WriteLine($"[socket] Room {roomId}: Resetting session");

            var session = GetGameSession(roomId);

            if (session != null)
            {
                // 잭팟으로 인한 초기화 로직 추가 가능
                var room = await _roomService.GetRoomByIdAsync(roomId);
                if (room != null)
                {
                    // 룸 세션 초기화
                    _roomSessions[room.RoomId] = GameSessionUtils.CreateNewSession(room);

                    // 타이머를 재시작하거나 필요한 추가 작업 수행
                    StopRoomTimer(roomId);
                    StartRoomTimer(room);
                }
                else
                {
                    Console.WriteLine($"[socket] Room {roomId} does not exist in session data.");
                }
            }
        }

        public void IncrementTotalUser(int roomId)
        {
            var session = GetGameSession(roomId);

            if (session != null)
            {
                lock (_lock)
                {
                    session.TotalUser++;
                    Console.WriteLine($"[socket] Room {roomId}: Total users updated to {session.TotalUser}");
                }
            }
        }

        public GameSession GetGameSession(int roomId)
        {
            _roomSessions.TryGetValue(roomId, out var session);

            if (session == null)
            {
                throw new Exception($"[socket] Room {roomId} does not exist in session data.");
            }

            return session;
        }

        public ConcurrentDictionary<int, GameSession> GetAllSessions()
        {
            return _roomSessions;
        }
    }
}
