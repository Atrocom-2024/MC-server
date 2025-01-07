using System.Net.Sockets;

using MC_server.GameRoom.Managers;
using MC_server.GameRoom.Models;
using MC_server.GameRoom.Service;
using MC_server.GameRoom.Utils;

namespace MC_server.GameRoom.Handlers
{
    public class ClientHandler
    {
        private readonly UserTcpService _userTcpService;
        private readonly ClientManager _clientManager;
        private readonly object _clientLock = new object();

        public ClientHandler(ClientManager clientManager, UserTcpService userTcpService)
        {
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _userTcpService = userTcpService ?? throw new ArgumentNullException(nameof(userTcpService));
        }

        //public async Task HandleClientAsync(TcpClient client)
        //{
        //    try
        //    {
        //        var networkStream = client.GetStream();

        //        while (client.Connected && SocketUtils.IsSocketConnected(client.Client))
        //        {
        //            var request = ProtobufUtils.DeserializeProtobuf<ClientRequest>(networkStream);

        //            if (request != null)
        //            {
        //                switch (request.RequestType)
        //                {
        //                    case "AddCoinsRequest":
        //                        if (request.AddCoinsData != null)
        //                        {
        //                            _ = HandleAddCoins(client, request.AddCoinsData);
        //                        }
        //                        break;
        //                    default:
        //                        Console.WriteLine("[socket] Unknown request type received.");
        //                        break;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[socket] HandleClientAsync in Error: {ex.Message}");
        //    }

        //    // 모든 경로에서 작업 완료
        //    await Task.CompletedTask;
        //}

        public async Task HandleAddCoins(TcpClient client, AddCoinsRequest addCoinsRequest)
        {
            // TODO: 유저가 받은 이익이 유저 자본의 10% 이상이 되면 Payout 초기화
            try
            {
                // 유저의 코인 수 변경
                var updatedUser = await _userTcpService.UpdateUserAsync(addCoinsRequest.UserId, "coins", addCoinsRequest.AddCoinsAmount);

                if (updatedUser != null)
                {
                    lock (_clientLock)
                    {
                        // 코인 추가 처리
                        _clientManager.UpdateGameUserState(client, "userTotalProfit", addCoinsRequest.AddCoinsAmount);
                    }
                    // 요청 클라이언트에게 응답 전송
                    var response = new ClientResponse
                    {
                        ResponseType = "AddCoinsResponse",
                        AddCoinsResponseData = new AddCoinsResponse { AddedCoinsAmount = updatedUser.Coins }
                    };
                    byte[] responseData = ProtobufUtils.SerializeProtobuf(response);
                    var stream = client.GetStream();
                    await stream.WriteAsync(responseData, 0, responseData.Length);
                }
                Console.WriteLine($"[socket] User Coins Added {addCoinsRequest.AddCoinsAmount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error handling adding coins: {ex.Message}");
            }
        }
    }
}
