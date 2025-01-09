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

        private readonly ClientManager _clientManager;
        private readonly RoomService _roomService;

        private readonly object _lock = new object();

        public GameRoomManager(ClientManager clientManager, RoomService roomService)
        {
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
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

            Timer timer = new Timer(async _ =>
            {
                // 잭팟이 터지거나 시간이 만료되면 초기화
                await ResetGameRoom(room.RoomId);
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
        private async Task ResetGameRoom(int roomId)
        {
            // TODO: 룸 초기화 시 해당 룸에 접속 중인 ClientManager의 _clientStates도 초기화 후 브로드캐스팅?
            // TODO: 잭팟이 터졌을 때 해당 룸 세션 초기화 기능 -> 다른 유저들에겐 TotalBetAmount의 10% 반환 후 페이아웃은 반환되지 않고 초기화
            Console.WriteLine($"[socket] Room {roomId}: Resetting session");

            var gameSession = GetGameSession(roomId);
            var clientsInRoom = _clientManager.GetClientsInRoom(roomId);

            if (gameSession != null)
            {
                // 잭팟으로 인한 초기화 로직 추가 가능
                var room = await _roomService.GetRoomByIdAsync(roomId);
                if (room != null)
                {
                    // 룸 세션 초기화 -> IsJackpot이 false이면 기존의 잭팟 금액 유지
                    if (!_roomSessions[room.RoomId].IsJackpot) // 잭팟이 터지지 않았을 때
                    {
                        long jackpotAmount = _roomSessions[room.RoomId].TotalJackpotAmount;
                        _roomSessions[room.RoomId] = GameSessionUtils.CreateNewSession(room);
                        _roomSessions[room.RoomId].TotalJackpotAmount = jackpotAmount;
                        _roomSessions[room.RoomId].TotalUser = clientsInRoom.Count();
                    }
                    else
                    {
                        _roomSessions[room.RoomId] = GameSessionUtils.CreateNewSession(room);
                        _roomSessions[room.RoomId].TotalUser = clientsInRoom.Count();
                    }
                    Console.WriteLine($"[socket] Room TotalUser {_roomSessions[room.RoomId].TotalUser}");

                    // 게임 유저 초기화
                    foreach (var client in clientsInRoom)
                    {
                        // TODO: 페이아웃 초기화 시 반환 기능 필요
                        _clientManager.UpdateGameUser(client, "currentPayout", 0M);
                        _clientManager.UpdateGameUser(client, "userTotalProfit", 0);
                        _clientManager.UpdateGameUser(client, "userTotalBetAmount", 0);
                        _clientManager.UpdateGameUser(client, "jackpotProb", 0.1M);
                    }

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
            var gameSession = GetGameSession(roomId);

            if (gameSession != null)
            {
                lock (_lock)
                {
                    gameSession.TotalUser++;
                    Console.WriteLine($"[socket] Room {roomId}: Total users updated to {gameSession.TotalUser}");
                }
            }
        }

        public GameSession GetGameSession(int roomId)
        {
            _roomSessions.TryGetValue(roomId, out var gameSession);

            if (gameSession == null)
            {
                throw new Exception($"[socket] Room {roomId} does not exist in session data.");
            }

            return gameSession;
        }

        public ConcurrentDictionary<int, GameSession> GetAllSessions()
        {
            return _roomSessions;
        }
    }
}
