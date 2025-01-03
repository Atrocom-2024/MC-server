using System.Net.Sockets;
using ProtoBuf;

using MC_server.GameRoom.Services;
using MC_server.GameRoom.Models;
using MC_server.GameRoom.Managers;

namespace MC_server.GameRoom.Handlers
{
    public class ClientHandler
    {
        private readonly GameRoomService _gameRoomService;
        private readonly ClientManager _clientManager;

        // GameSession에 대한 동기화 제어를 위해 사용됨 -> 다수의 스레드가 동시에 GameSession을 읽거나 수정하려고 할 때 충돌을 방지
        private readonly object _sessionLock = new object();

        public ClientHandler(GameRoomService gameRoomService, ClientManager clientManager)
        {
            _gameRoomService = gameRoomService ?? throw new ArgumentNullException(nameof(gameRoomService));
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
        }

        public async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var networkStream = client.GetStream(); // TCP 클라이언트의 네트워크 스트림을 가져옴

                while (true)
                {
                    // 클라이언트로부터 데이터를 비동기적으로 읽기
                    var request = DeserializeProtobuf<ClientRequest>(networkStream);

                    // reqeust가 유효하고, 클라이언트가 특정 룸에 연결되어 있는 경우에만 통과
                    if (request != null) 
                    {
                        switch (request.RequestType)
                        {
                            case "JoinRoom":
                                if (request.JoinRoomData != null)
                                {
                                    HandleJoinRoom(client, request.JoinRoomData);
                                }
                                break;
                            case "Bet":
                                if (request.BetData != null)
                                {
                                    HandleBetting(client, request.BetData);
                                }
                                break;
                            default:
                                Console.WriteLine("[socket] Unknown request type received.");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error: {ex.Message}");
            }
            finally
            {
                _clientManager.RemoveClient(client); // 클라이언트를 관리 목록에서 제거
                client.Close();
                Console.WriteLine("[socket] Connection closed");
            }

            // 모든 경로에서 작업 완료
            await Task.CompletedTask;
        }

        private void HandleJoinRoom(TcpClient client, JoinRoomRequest joinRequest)
        {
            try
            {
                _clientManager.AssignClientToRoom(client, joinRequest.UserId, joinRequest.RoomId);
                Console.WriteLine($"[socket] Client assigned to Room {joinRequest.RoomId}");

                // 유저가 게임에 조인 후 해당 게임 유저 정보를 응답
                var gameUserState = _clientManager.GetGameUserState(client);
                if (gameUserState != null)
                {
                    var responseData = new ClientResponse
                    {
                        ResponseType = "GameUserState",
                        GameUserState = gameUserState
                    };
                    var networkStream = client.GetStream();
                    byte[] serializeResponseData = SerializeProtobuf(responseData);
                    networkStream.Write(serializeResponseData, 0, serializeResponseData.Length);
                    Console.WriteLine($"[socket] Sent user state to client: {joinRequest.UserId}");
                }

                // 초기 세션 데이터 전달 -> 해당 코드에서 유저 새로 접속 시 payout을 재계산해서 브로드캐스트하는 로직 추가 필요
                var session = _gameRoomService.GetSession(joinRequest.RoomId);
                if (session != null)
                {
                    BroadcastGameState(joinRequest.RoomId, session);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error handling room join: {ex.Message}");
            }
        }

        private void HandleBetting(TcpClient client, BetRequest betRequest)
        {
            try
            {
                int roomId = _clientManager.GetRoomId(client);
                var session = _gameRoomService.GetSession(roomId);

                if (session != null)
                {
                    lock (_sessionLock) // GameSession 업데이트 보호
                    {
                        // 배팅 처리
                        session.TotalBetAmount += betRequest.BetAmount;
                        session.TotalJackpotAmount += (long)Math.Round(betRequest.BetAmount * 0.1);
                    }
                    Console.WriteLine($"[socket] Room {roomId}: TotalBet = {session.TotalBetAmount}");

                    // 변경된 세션 데이터 브로드캐스트
                    BroadcastGameState(roomId, session);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error handling betting: {ex.Message}");
            }
        }

        private void BroadcastGameState(int roomId, GameSession session)
        {
            lock ( _sessionLock) // GameSession 읽기 보호
            {
                // 1. 응답 데이터 생성
                var responseData = new ClientResponse
                {
                    ResponseType = "GameState",
                    GameState = session
                };

                // 2. 응답 데이터 직렬화
                byte[] protobufMessage = SerializeProtobuf(responseData);

                // 3. 해당 룸에 연결된 클라이언트 가져오기
                var clientsInRoom = _clientManager.GetClientsInRoom(roomId);

                // 4. 클라이언트들에게 데이터 전송
                foreach (var client in clientsInRoom)
                {
                    try
                    {
                        if (client.Connected)
                        {
                            var stream = client.GetStream();
                            stream.Write(protobufMessage, 0, protobufMessage.Length);
                            Console.WriteLine($"[socket] Broadcasted to client: Room {roomId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[socket] Error broadcasting to client in Room {roomId}: {ex.Message}");
                    }
                }
            }
        }

        private byte[] SerializeProtobuf<Task>(Task obj)
        {
            using var memoryStream = new MemoryStream();
            Serializer.SerializeWithLengthPrefix(memoryStream, obj, PrefixStyle.Base128);
            return memoryStream.ToArray();
        }

        private T DeserializeProtobuf<T>(NetworkStream networkStream)
        {
            return Serializer.DeserializeWithLengthPrefix<T>(networkStream, PrefixStyle.Base128);
        }
    }
}
