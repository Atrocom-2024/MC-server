using System.Net.Sockets;

using MC_server.GameRoom.Managers;
using MC_server.GameRoom.Models;
using MC_server.GameRoom.Service;
using MC_server.GameRoom.Utils;

namespace MC_server.GameRoom.Communication
{
    public class BroadcastMessageSender
    {
        private readonly ClientManager _clientManager;
        private readonly UserTcpService _userTcpService;

        public BroadcastMessageSender(ClientManager clientManager, UserTcpService userTcpService)
        {
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _userTcpService = userTcpService ?? throw new ArgumentNullException(nameof(userTcpService));
        }

        /// <summary>
        /// clients에 속한 모든 클라이언트에게 response를 브로드캐스트
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static async Task BroadcastToClients(IEnumerable<TcpClient> clients, ClientResponse response)
        {
            byte[] responseData = ProtobufUtils.SerializeProtobuf(response);

            foreach (var client in clients)
            {
                if (client.Connected)
                {
                    try
                    {
                        var stream = client.GetStream();
                        await stream.WriteAsync(responseData, 0, responseData.Length);
                        await stream.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[socket] Error broadcasting to client: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// roomId에 속한 모든 클라이언트에게 GameUserState를 브로드캐스트
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public async Task BroadcastUserState(int roomId)
        {
            var clients = _clientManager.GetClientsInRoom(roomId);

            foreach(var client in clients)
            {
                var gameUser = _clientManager.GetGameUser(client);
                var responseData = new ClientResponse
                {
                    ResponseType = "GameUserState",
                    GameUserState = new GameUserState
                    {
                        CurrentPayout = gameUser.CurrentPayout,
                        JackpotProb = gameUser.JackpotProb,
                    }
                };

                // 공통 브로드캐스트 메서드 호출
                await BroadcastToClients([client], responseData);
            }
        }

        /// <summary>
        /// roomId에 속한 모든 클라이언트에게 GameState를 브로드캐스트
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="gameState"></param>
        /// <returns></returns>
        public async Task BroadcastGameState(int roomId, GameState gameState)
        {
            var clients = _clientManager.GetClientsInRoom(roomId);
            var responseData = new ClientResponse
            {
                ResponseType = "GameState",
                GameState = gameState
            };

            await BroadcastToClients(clients, responseData);
        }

        /// <summary>
        /// 게임 세션 종료 시 roomId에 속한 모든 클라이언트에게 리워드 코인 브로드캐스트
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public async Task BroadcastGameSessionEnd(int roomId, TcpClient? jackpotClient = null)
        {
            Console.WriteLine($"[socket] Resetting game session for room {roomId}");
            var clients = _clientManager.GetClientsInRoom(roomId);

            foreach (var client in clients)
            {
                if (jackpotClient != null && client == jackpotClient)
                {
                    continue;
                }
                var gameUser = _clientManager.GetGameUser(client);
                var rewardCoins = (int)(gameUser.UserSessionBetAmount * 0.1M);
                var updatedUser = await _userTcpService.UpdateUserAsync(gameUser.UserId, "coins", rewardCoins);
                
                if (updatedUser != null)
                {
                    var responseData = new ClientResponse
                    {
                        ResponseType = "GameSessionEnd",
                        GameSessionEndData = new GameSessionEnd
                        {
                            RewardedCoinsAmount = updatedUser.Coins,
                            RewardCoins = rewardCoins
                        }
                    };
                    await BroadcastToClients([client], responseData);
                }
            }
        }
    }
}
