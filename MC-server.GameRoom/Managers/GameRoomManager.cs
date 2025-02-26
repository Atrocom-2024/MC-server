using System.Collections.Concurrent;

using MC_server.GameRoom.Managers.Models;
using MC_server.GameRoom.Utils;
using MC_server.GameRoom.Service;
using MC_server.GameRoom.Models;

namespace MC_server.GameRoom.Managers
{
    public class GameRoomManager
    {
        // 각 게임 룸의 현재 세션 정보를 관리 -> 키는 룸 id, 값은 해당 룸의 세션 데이터
        private readonly ConcurrentDictionary<int, GameSession> _gameSessions = new ConcurrentDictionary<int, GameSession>();
        // 각 룸별 타이머 관리
        private readonly ConcurrentDictionary<int, Timer> _sessionTimers = new ConcurrentDictionary<int, Timer>();

        private readonly ClientManager _clientManager;
        private readonly UserTcpService _userTcpService;
        private readonly GameTcpService _gameTcpService;

        private readonly object _lock = new object();

        public GameRoomManager(ClientManager clientManager, UserTcpService userTcpService, GameTcpService gameTcpService)
        {
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _userTcpService = userTcpService ?? throw new ArgumentNullException(nameof(userTcpService));
            _gameTcpService = gameTcpService ?? throw new ArgumentNullException(nameof(gameTcpService));
        }

        public async Task InitializeRooms()
        {
            // DB에서 룸의 기본 정보를 모두 받아옴
            var allRooms = await _gameTcpService.GetAllRoomsAsync();

            // 데이터 출력
            if (allRooms != null && allRooms.Count > 0)
            {
                foreach (var room in allRooms)
                {
                    var gameRecord = await _gameTcpService.GetGameRecordByIdAsync(room.RoomId);

                    // 게임 세션 초기화
                    _gameSessions[room.RoomId] = GameSessionUtils.CreateNewSession(room);

                    if (gameRecord != null)
                    {
                        _gameSessions[room.RoomId].TotalJackpotAmount = gameRecord.TotalJackpotAmount;
                    }

                    // 타이머 초기화
                    StartRoomTimer(room.RoomId);
                }
            }
            Console.WriteLine("[socket] Initialized 10 game rooms");
        }

        // 특정 룸의 타이머 시작
        public void StartRoomTimer(int roomId)
        {
            if (_sessionTimers.ContainsKey(roomId))
            {
                Console.WriteLine($"[socket] Timer for Room {roomId} is already running.");
                return;
            }

            Timer timer = new Timer(async _ =>
            {
                // 잭팟이 터지거나 시간이 만료되면 초기화
                await ResetGameRoom(roomId);
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _sessionTimers[roomId] = timer;
            Console.WriteLine($"[socket] Timer started for Room {roomId}");
        }

        // 특정 룸의 타이머 중단
        public void StopRoomTimer(int roomId)
        {
            if (_sessionTimers.TryRemove(roomId, out var timer))
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
            // TODO: 잭팟이 터졌을 때 해당 룸 세션 초기화 기능 -> 다른 유저들에겐 TotalBetAmount의 10% 반환 후 페이아웃은 반환되지 않고 초기화
            Console.WriteLine($"[socket] Room {roomId}: Resetting session");

            // 게임이 초기화 될 때 초기화될 게임 세션의 데이터를 저장 -> 게임 결과 기록 목적
            await _gameTcpService.RecordGameResult(roomId, _gameSessions[roomId]);

            var clientsInRoom = _clientManager.GetClientsInRoom(roomId);

            // 잭팟으로 인한 초기화 로직 추가 가능
            var room = await _gameTcpService.GetRoomByIdAsync(roomId);
            var existingGameRecord = await _gameTcpService.GetGameRecordByIdAsync(roomId);

            if (room != null && existingGameRecord != null)
            {
                // 룸 세션 초기화 -> IsJackpot이 false이면 기존의 잭팟 금액 유지
                _gameSessions[roomId] = GameSessionUtils.CreateNewSession(room);
                _gameSessions[roomId].TotalUser = clientsInRoom.Count();

                if (!existingGameRecord.IsJackpot) // 잭팟이 터지지 않았을 때
                {
                    _gameSessions[roomId].TotalJackpotAmount = existingGameRecord.TotalJackpotAmount;
                }

                await EndGameRewardPayment(roomId); // 게임 세션 초기화 시 페이아웃 반환

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

        private async Task EndGameRewardPayment(int roomId)
        {
            var clientsInRoom = _clientManager.GetClientsInRoom(roomId);

            foreach (var client in clientsInRoom)
            {
                var gameUser = _clientManager.GetGameUser(client);
                var rewardCoins = (int)(gameUser.UserTotalBetAmount * 0.1M);
                var updatedUser = await _userTcpService.UpdateUserAsync(gameUser.UserId, "coins", rewardCoins);

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
                gameSession.TotalUser++;
                Console.WriteLine($"[socket] Room {roomId}: Total users updated to {gameSession.TotalUser}");
            }
        }

        public GameSession GetGameSession(int roomId)
        {
            _gameSessions.TryGetValue(roomId, out var gameSession);

            if (gameSession == null)
            {
                throw new Exception($"[socket] Room {roomId} does not exist in session data.");
            }

            return gameSession;
        }

        public ConcurrentDictionary<int, GameSession> GetAllSessions()
        {
            return _gameSessions;
        }
    }
}
