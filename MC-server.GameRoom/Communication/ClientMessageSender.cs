using System.Net.Sockets;

using MC_server.GameRoom.Models;
using MC_server.GameRoom.Utils;

namespace MC_server.GameRoom.Communication
{
    public class ClientMessageSender
    {
        public static async Task SendToClient(TcpClient client, ClientResponse response)
        {
            byte[] responseData = ProtobufUtils.SerializeProtobuf(response);
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
                    Console.WriteLine($"[socket] Error sending to client: {ex.Message}");
                }
            }
        }

        public async Task SendBetResponse(TcpClient client, BetResponse betResponse)
        {
            var responseData = new ClientResponse
            {
                ResponseType = "BetResponse",
                BetResponseData = betResponse
            };

            await SendToClient(client, responseData);
        }

        public async Task SendAddCoinsResponse(TcpClient client, AddCoinsResponse addCoinsResponse)
        {
            var responseData = new ClientResponse
            {
                ResponseType = "AddCoinsResponse",
                AddCoinsResponseData = addCoinsResponse
            };
            await SendToClient(client, responseData);
        }

        public async Task SendJackpotWinResponse(TcpClient client, JackpotWinResponse jackpotWinResponse)
        {
            var responseData = new ClientResponse
            {
                ResponseType = "JackpotWinResponse",
                JackpotWinResponseData = jackpotWinResponse
            };
            await SendToClient(client, responseData);
        }

        public async Task SendGameState(TcpClient client, GameState gameState)
        {
            var responseData = new ClientResponse
            {
                ResponseType = "GameState",
                GameState = gameState
            };
            await SendToClient(client, responseData);
        }

        public async Task SendGameUserState(TcpClient client, GameUserState gameUserState)
        {
            var responseData = new ClientResponse
            {
                ResponseType = "GameUserState",
                GameUserState = gameUserState
            };
            await SendToClient(client, responseData);
        }

        public async Task SendErrorResponse(TcpClient client, string responseType, string errorMessage)
        {
            var responseData = new ClientResponse
            {
                ResponseType = responseType,
                ErrorMessage = errorMessage
            };
            await SendToClient(client, responseData);
        }
    }
}
