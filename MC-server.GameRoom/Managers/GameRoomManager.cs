using System.Collections.Concurrent;

using MC_server.GameRoom.Managers.Models;
using MC_server.GameRoom.Utils;
using MC_server.GameRoom.Service;
using MC_server.GameRoom.Communication;
using MC_server.GameRoom.Enum;
using System.Net.Sockets;

namespace MC_server.GameRoom.Managers
{
    public class GameRoomManager
    {
        // 각 게임 룸의 현재 세션 정보를 관리 -> 키는 룸 id, 값은 해당 룸의 세션 데이터
        private readonly ConcurrentDictionary<int, GameSession> _gameSessions = new ConcurrentDictionary<int, GameSession>();
        // 각 룸별 타이머 관리
        private readonly ConcurrentDictionary<int, Timer> _sessionTimers = new ConcurrentDictionary<int, Timer>();

        private readonly ClientManager _clientManager;
        private readonly BroadcastMessageSender _broadcastMessageSender;
        private readonly GameTcpService _gameTcpService;

        public GameRoomManager(ClientManager clientManager, BroadcastMessageSender broadcastMessageSender, GameTcpService gameTcpService)
        {
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _broadcastMessageSender = broadcastMessageSender ?? throw new ArgumentNullException(nameof(broadcastMessageSender));
            _gameTcpService = gameTcpService ?? throw new ArgumentNullException(nameof(gameTcpService));
        }

        /// <summary>
        /// 특정 룸 세션의 잭팟 상태 변경 메서드
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="jackpotState"></param>
        public void ChangedJackpotState(int roomId, bool jackpotState)
        {
            _gameSessions.AddOrUpdate(roomId,
                key => new GameSession { IsJackpot = jackpotState }, // 값이 없으면 추가
                (key, existingGameSession) =>
                {
                    existingGameSession.IsJackpot = jackpotState; // 값이 있으면 업데이트
                    return existingGameSession;
                }
            );
        }

        /// <summary>
        /// 게임 세션 데이터 복사 메서드
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public GameSession CloneGameSession(int roomId)
        {
            return new GameSession
            {
                GameId = _gameSessions[roomId].GameId,
                TotalBetAmount = _gameSessions[roomId].TotalBetAmount,
                TotalUser = _gameSessions[roomId].TotalUser,
                IsJackpot = _gameSessions[roomId].IsJackpot,
                TargetPayout = _gameSessions[roomId].TargetPayout,
                MaxBetAmount = _gameSessions[roomId].MaxBetAmount,
                MaxUser = _gameSessions[roomId].MaxUser,
                CreatedAt = _gameSessions[roomId].CreatedAt
            };
        }
        
        /// <summary>
        /// 모든 룸 세션을 초기화 메서드
        /// </summary>
        /// <returns></returns>
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

                    // 타이머 초기화
                    StartRoomTimer(room.RoomId);
                }
            }
        }

        /// <summary>
        /// 특정 룸 세션 타이머 시작
        /// </summary>
        /// <param name="roomId"></param>
        public void StartRoomTimer(int roomId)
        {
            if (_sessionTimers.ContainsKey(roomId))
            {
                Console.WriteLine($"[socket] Timer for Room {roomId} is already running.");
                return;
            }

            //if (roomId == 5)
            //{
            //    // 세션 초기화까지의 총 시간(초)
            //    const int totalSeconds = 60;
            //    // 남은 시간 변수 초기화
            //    int remainingSeconds = totalSeconds;

            //    // 1초마다 로그를 남길 카운트다운 타이머 생성
            //    Timer countdownTimer = new Timer(state =>
            //    {
            //        // 남은 시간 로그 출력
            //        Console.WriteLine($"[socket] Room {roomId}: 세션 초기화까지 남은 초: {remainingSeconds}");
            //        remainingSeconds--;

            //        // 남은 시간이 0 이하가 되면 카운트다운 타이머 종료
            //        if (remainingSeconds < 0)
            //        {
            //            ((Timer)state)?.Dispose();
            //        }
            //    }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            //}

            _sessionTimers.AddOrUpdate(roomId, new Timer(async _ =>
            {
                // 잭팟이 터지거나 시간이 만료되면 초기화
                await ResetGameRoom(roomId);
            }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)), (key, oldValue) => oldValue);
        }

        /// <summary>
        /// 특정 룸 세션 초기화 메서드
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public async Task ResetGameRoom(int roomId, TcpClient? jackpotClient = null)
        {
            // TODO: 잭팟으로 인해 세션 초기화 시 TotalBetAmount의 10% 반환 후 페이아웃은 반환되지 않고 초기화

            // 잭팟으로 인한 초기화 로직 추가 가능
            var tempGameSession = CloneGameSession(roomId); // 게임 세션 데이터 복사
            var clientsInRoom = _clientManager.GetClientsInRoom(roomId);
            var room = await _gameTcpService.GetRoomByIdAsync(roomId);

            if (room == null)
            {
                Console.WriteLine($"[socket] Room {roomId} does not exist in session data.");
                return;
            }

            // 1. 게임 세션 초기화
            _gameSessions[roomId] = GameSessionUtils.CreateNewSession(room);
            _gameSessions[roomId].TotalUser = clientsInRoom.Count();
            
            // 2. 세션 종료 리워드 브로드캐스트
            await _broadcastMessageSender.BroadcastGameSessionEnd(roomId, jackpotClient);
            
            try
            {
                var gameSession = GetGameSession(roomId);

                // 3. 게임 유저 초기화 및 브로드캐스트
                foreach (var client in clientsInRoom)
                {
                    if (jackpotClient != null && client == jackpotClient)
                    {
                        await _clientManager.ResetGameUser(client, gameSession, ResetLevel.Hard);
                    }
                    else
                    {
                        await _clientManager.ResetGameUser(client, gameSession, ResetLevel.Soft);
                    }
                }
                await _broadcastMessageSender.BroadcastUserState(roomId); // 유저 상태 브로드캐스트
            
                // 5. 게임이 초기화 될 때 초기화될 게임 세션의 데이터를 저장 -> 게임 결과 기록 목적
                await _gameTcpService.RecordGameResult(roomId, tempGameSession);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket][ResetGameRoom] Error broadcasing to client: {ex.Message}");
            }
            
            // 타이머 재시작
            StopRoomTimer(roomId);
            StartRoomTimer(roomId);
        }

        /// <summary>
        /// 특정 룸 세션 타이머 중단 메서드
        /// </summary>
        /// <param name="roomId"></param>
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

        public void IncrementTotalUser(int roomId)
        {
            var gameSession = GetGameSession(roomId);

            if (gameSession != null)
            {
                gameSession.TotalUser++;
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
