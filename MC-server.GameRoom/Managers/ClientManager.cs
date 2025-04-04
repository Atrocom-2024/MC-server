using System.Net.Sockets;
using System.Collections.Concurrent;

using MC_server.GameRoom.Managers.Models;
using MC_server.GameRoom.Utils;
using MC_server.GameRoom.Service;
using MC_server.GameRoom.Models;
using MC_server.GameRoom.Communication;
using MC_server.GameRoom.Enum;

namespace MC_server.GameRoom.Managers
{
    public class ClientManager
    {
        private readonly ClientMessageSender _clientMessageSender;
        private readonly UserTcpService _userTcpService;

        // 현재 연결된 모든 클라이언트를 관리, 각 클라이언트가 어느 룸에 연결되어 있는지 추적 -> 키는 클라이언트 객체, 값은 해당 클라이언트가 속한 룸의 id
        private readonly ConcurrentDictionary<TcpClient, GameUser> _clientStates = new ConcurrentDictionary<TcpClient, GameUser>();

        public ClientManager(ClientMessageSender clientMessageSender, UserTcpService userTcpService)
        {
            _clientMessageSender = clientMessageSender ?? throw new ArgumentNullException(nameof(clientMessageSender));
            _userTcpService = userTcpService ?? throw new ArgumentNullException(nameof(userTcpService));
        }

        public async Task<GameUser> AddClient(TcpClient client, string userId, int roomId)
        {
            // 기존 userId가 있는 TcpClient 찾기
            var existingEntry = _clientStates.FirstOrDefault(x => x.Value.UserId == userId);
            if (!existingEntry.Equals(default(KeyValuePair<TcpClient, GameUser>)))
            {
                existingEntry.Key.Close();
                _clientStates.TryRemove(existingEntry.Key, out _);
            }

            var user = await _userTcpService.GetUserByIdAsync(userId) ?? throw new Exception("User can not found");

            var gameUser = new GameUser
            {
                UserId = userId,
                RoomId = roomId,
                BetCount = 0,
                CurrentPayout = 0.0M,
                InitialCoins = user.Coins,
                UserTotalProfit = 0,
                UserTotalBetAmount = 0,
                UserSessionBetAmount = 0,
                JackpotProb = 0.0M
            };

            // _clientStates에 동일한 TcpClient 키를 덮어쓸 경우 예기치 않은 동작이 발생할 수 있어 메서드를 통한 추가
            _clientStates.AddOrUpdate(client, gameUser, (key, existingValue) => gameUser);

            return gameUser;
        }

        public void RemoveClient(TcpClient client)
        {
            //Console.WriteLine($"[socket] Client disconnected: {client.Client.RemoteEndPoint}");
            _clientStates.TryRemove(client, out _);
        }

        public Dictionary<string, object> UpdateGameUser(TcpClient client, string property, object value)
        {
            if (_clientStates.TryGetValue(client, out var gameUser))
            {
                var updatedData = new Dictionary<string, object>();

                switch (property)
                {
                    case "betCount":
                        if (value is int count)
                        {
                            gameUser.BetCount += count;
                            updatedData["betCount"] = gameUser.BetCount;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for CurrentPayout.");
                        }
                        break;
                    case "currentPayout":
                        if (value is decimal newPayout)
                        {
                            gameUser.CurrentPayout = newPayout;
                            updatedData["currentPayout"] = gameUser.CurrentPayout;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for CurrentPayout.");
                        }
                        break;
                    case "userTotalProfit":
                        if (value is int addCoinsAmount)
                        {
                            gameUser.UserTotalProfit += addCoinsAmount;
                            updatedData["userTotalProfit"] = gameUser.UserTotalProfit;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for UserTotalProfit.");
                        }
                        break;
                    case "userTotalBetAmount":
                        if (value is int betAmount)
                        {
                            gameUser.UserTotalBetAmount += betAmount;
                            updatedData["userTotalBetAmount"] = gameUser.UserTotalBetAmount;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for UserTotalBetAmount");
                        }
                        break;
                    case "userSessionBetAmount":
                        if (value is int sessionBetAmount)
                        {
                            gameUser.UserSessionBetAmount += sessionBetAmount;
                            updatedData["userSessionBetAmount"] = gameUser.UserSessionBetAmount;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for UserSessionBetAmount");
                        }
                        break;
                    case "jackpotProb":
                        if (value is decimal newJackpotProb)
                        {
                            gameUser.JackpotProb = newJackpotProb;
                            updatedData["jackpotProb"] = gameUser.JackpotProb;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for JackpotProb");
                        }
                        break;
                    default:
                        throw new ArgumentException($"Property '{property}' is not a valid GameUserState property.");
                    }

                return updatedData;
            }
            else
            {
                throw new InvalidOperationException("Client not found or not assigned to any room.");
            }
        }

        public async Task ResetGameUser(TcpClient client, GameSession gameSession, ResetLevel level)
        {

            if (_clientStates.TryGetValue(client, out var gameUser))
            {
                var user = await _userTcpService.GetUserByIdAsync(gameUser.UserId) ?? throw new Exception("User can not found");

                gameUser.CurrentPayout = 0M;
                gameUser.InitialCoins = user.Coins;
                gameUser.UserTotalProfit = 0;
                gameUser.UserTotalBetAmount = 0;
                gameUser.UserSessionBetAmount = 0;

                // 베팅 횟수, 잭팟 확률은 하드 리셋일 때만 초기화
                if (level == ResetLevel.Hard)
                {
                    gameUser.BetCount = 0;
                    gameUser.JackpotProb = 0.0M;
                }

                var newPayout = GameUserStateUtils.CalculatePayout(gameUser, gameSession);
                UpdateGameUser(client, "currentPayout", newPayout);
            }
        }

        public void UpdatePayout(TcpClient client, GameSession gameSession)
        {
            if (_clientStates.TryGetValue(client, out var gameUser))
            {
                var newPayout = GameUserStateUtils.CalculatePayout(gameUser, gameSession);
                UpdateGameUser(client, "currentPayout", newPayout);
            }
        }

        public void UpdateJackpotProb(TcpClient client)
        {
            if (_clientStates.TryGetValue(client, out var gameUser))
            {
                var newJackpotProb = GameUserStateUtils.CalculateJackpotProb(gameUser);
                UpdateGameUser(client, "jackpotProb", newJackpotProb);
            }
        }

        public async Task CheckAndResetPayout(TcpClient client, GameSession gameSession)
        {
            var gameUser = GetGameUser(client);

            if (gameUser.InitialCoins > 0)
            {
                var userProfit = (decimal)gameUser.UserTotalProfit / gameUser.InitialCoins;

                // 10% 초과 여부 확인
                if (userProfit > 0.1m)
                {
                    gameUser.UserTotalBetAmount = 0; // 총 배팅 금액을 0으로 초기화를 시켜 payout 초기화

                    var newPayout = GameUserStateUtils.CalculatePayout(gameUser, gameSession);
                    UpdateGameUser(client, "currentPayout", newPayout);

                    var gameUserState = new GameUserState
                    {
                        CurrentPayout = gameUser.CurrentPayout,
                        JackpotProb = gameUser.JackpotProb,
                    };
                    await _clientMessageSender.SendGameUserState(client, gameUserState);
                }
            }
        }

        // 특정 클라이언트의 정보를 반환하는 메서드
        public GameUser GetGameUser(TcpClient client)
        {
            if (_clientStates.TryGetValue(client, out var gameUser))
            {
                return gameUser;
            }

            throw new InvalidOperationException("Client not found or not assigned to any room.");
        }

        // 특정 클라이언트가 어떤 룸에 참여 중인지 반환하는 메서드
        public int GetUserRoomId(TcpClient client)
        {
            if (_clientStates.TryGetValue(client, out var gameUser))
            {
                return gameUser.RoomId;
            }

            throw new InvalidOperationException("Client not assigned to any room.");
        }

        // 특정 룸에 연결된 클라이언트 정보를 반환하는 메서드
        public IEnumerable<TcpClient> GetClientsInRoom(int roomId)
        {
            return _clientStates.Where(pair => pair.Value.RoomId == roomId).Select(pair => pair.Key);
        }
        
        public ConcurrentDictionary<TcpClient, GameUser> GetAllClients()
        {
            return _clientStates;
        }
    }
}
