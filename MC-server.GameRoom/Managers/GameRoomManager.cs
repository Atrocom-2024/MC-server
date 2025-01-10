using System.Collections.Concurrent;

using MC_server.Core.Services;
using MC_server.GameRoom.Managers.Models;
using MC_server.GameRoom.Utils;
using MC_server.GameRoom.Service;
using MC_server.GameRoom.Models;
using MC_server.GameRoom.Handlers;

namespace MC_server.GameRoom.Managers
{
    public class GameRoomManager
    {
        // 각 게임 룸의 현재 세션 정보를 관리 -> 키는 룸 id, 값은 해당 룸의 세션 데이터
        private readonly ConcurrentDictionary<int, GameSession> _roomSessions = new ConcurrentDictionary<int, GameSession>();
        // 각 룸별 타이머 관리
        private readonly ConcurrentDictionary<int, Timer> _roomTimers = new ConcurrentDictionary<int, Timer>();

        private readonly ClientManager _clientManager;
        private readonly UserTcpService _userTcpService;
        private readonly RoomService _roomService;

        private readonly object _lock = new object();

        public GameRoomManager(ClientManager clientManager, UserTcpService userTcpService, RoomService roomService)
        {
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _userTcpService = userTcpService ?? throw new ArgumentNullException(nameof(clientManager));
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
                    StartRoomTimer(room.RoomId);
                }
            }
            Console.WriteLine("[socket] Initialized 10 game rooms");
        }

        // 특정 룸의 타이머 시작
        public void StartRoomTimer(int roomId)
        {
            if (_roomTimers.ContainsKey(roomId))
            {
                Console.WriteLine($"[socket] Timer for Room {roomId} is already running.");
                return;
            }

            Timer timer = new Timer(async _ =>
            {
                // 잭팟이 터지거나 시간이 만료되면 초기화
                await ResetGameRoom(roomId);
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _roomTimers[roomId] = timer;
            Console.WriteLine($"[socket] Timer started for Room {roomId}");
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

            var clientsInRoom = _clientManager.GetClientsInRoom(roomId);

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

                await ReturnPayout(roomId); // 게임 세션 초기화 시 페이아웃 반환

                // 게임 유저 초기화 및 브로드캐스트
                try
                {
                    var gameSession = GetGameSession(roomId);

                    foreach (var client in clientsInRoom)
                    {
                        _clientManager.ResetGameUser(client, gameSession);
                        var gameUser = _clientManager.GetGameUser(client);
                        var clientResponse = new ClientResponse
                        {
                            ResponseType = "GameUserState",
                            GameUserState = new GameUserState
                            {
                                CurrentPayout = gameUser.CurrentPayout,
                                JackpotProb = gameUser.JackpotProb,
                            }
                        };
                        byte[] responseData = ProtobufUtils.SerializeProtobuf(clientResponse);

                        if (client.Connected)
                        {
                            var stream = client.GetStream();
                            stream.Write(responseData, 0, responseData.Length);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[socket][ResetGameRoom] Error broadcasing to client: {ex.Message}");
                }

                // 타이머 재시작
                StopRoomTimer(roomId);
                StartRoomTimer(roomId);
            }
            else
            {
                Console.WriteLine($"[socket] Room {roomId} does not exist in session data.");
            }
        }

        private async Task ReturnPayout(int roomId)
        {
            var clientsInRoom = _clientManager.GetClientsInRoom(roomId);

            foreach (var client in clientsInRoom)
            {
                var gameUser = _clientManager.GetGameUser(client);
                var updatedUser = await _userTcpService.UpdateUserAsync(gameUser.UserId, "coins", (int)(gameUser.UserTotalBetAmount * gameUser.CurrentPayout));

                if (updatedUser != null)
                {
                    var response = new ClientResponse
                    {
                        ResponseType = "AddCoinsResponse",
                        AddCoinsResponseData = new AddCoinsResponse { AddedCoinsAmount = updatedUser.Coins }
                    };
                    byte[] responseData = ProtobufUtils.SerializeProtobuf(response);
                    var stream = client.GetStream();
                    await stream.WriteAsync(responseData, 0, responseData.Length);
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
