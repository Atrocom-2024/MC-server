using System.Net.Sockets;

using MC_server.GameRoom.Models;
using MC_server.GameRoom.Managers;
using MC_server.GameRoom.Service;
using MC_server.GameRoom.Utils;

namespace MC_server.GameRoom.Handlers
{
    public class GameRoomHandler
    {
        private readonly GameRoomManager _gameRoomManager;
        private readonly ClientManager _clientManager;
        private readonly ClientHandler _clientHandler;

        private readonly UserTcpService _userTcpService;

        // GameSession에 대한 동기화 제어를 위해 사용됨 -> 다수의 스레드가 동시에 GameSession을 읽거나 수정하려고 할 때 충돌을 방지
        private readonly object _sessionLock = new object();

        public GameRoomHandler(GameRoomManager gameRoomManager, ClientManager clientManager, UserTcpService userTcpService, ClientHandler clientHandler)
        {
            _gameRoomManager = gameRoomManager ?? throw new ArgumentNullException(nameof(gameRoomManager));
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _userTcpService = userTcpService ?? throw new ArgumentNullException(nameof(userTcpService));
            _clientHandler = clientHandler ?? throw new ArgumentNullException(nameof(_clientHandler));
        }

        public async Task HandleGameRoomAsync(TcpClient client)
        {
            // TODO: 여기서 모든 요청을 처리하도록 수정해야 함. ClientHandler에서 처리하면 안됨.
            try
            {
                var networkStream = client.GetStream(); // TCP 클라이언트의 네트워크 스트림을 가져옴

                while (client.Connected && SocketUtils.IsSocketConnected(client.Client))
                {
                    // 클라이언트로부터 데이터를 비동기적으로 읽기
                    var request = ProtobufUtils.DeserializeProtobuf<ClientRequest>(networkStream);

                    // reqeust가 유효하고, 클라이언트가 특정 룸에 연결되어 있는 경우에만 통과
                    if (request != null) 
                    {
                        switch (request.RequestType)
                        {
                            case "JoinRoomRequest":
                                if (request.JoinRoomData != null)
                                {
                                    HandleJoinRoom(client, request.JoinRoomData);
                                }
                                break;
                            case "BetRequest":
                                if (request.BetData != null)
                                {
                                    _ = HandleBetting(client, request.BetData);
                                }
                                break;
                            case "AddCoinsRequest":
                                if (request.AddCoinsData != null)
                                {
                                    _ = _clientHandler.HandleAddCoins(client, request.AddCoinsData);
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
                Console.WriteLine($"[socket] HandleGameRoomAsync Error: {ex.Message}");
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
            // TODO: 유저가 해당 룸에 Join 할 때마다 모든 유저의 페이아웃을 재계산 후 브로드캐스트 기능
            try
            {
                Console.WriteLine($"Join User ID: {joinRequest.UserId}");
                // 유저가 해당 룸에 Join 시 해당 룸에 유저 정보 등록
                _clientManager.AddClient(client, joinRequest.UserId, joinRequest.RoomId);
                Console.WriteLine($"[socket] Client assigned to Room {joinRequest.RoomId}");

                // 유저가 해당 룸에 Join 할 때마다 해당 게임의 TotalUser + 1
                _gameRoomManager.IncrementTotalUser(joinRequest.RoomId);

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
                    byte[] serializeResponseData = ProtobufUtils.SerializeProtobuf(responseData);
                    networkStream.Write(serializeResponseData, 0, serializeResponseData.Length);
                    Console.WriteLine($"[socket] Sent user state to client: {joinRequest.UserId}");
                }

                // 초기 세션 데이터 전달 -> 해당 코드에서 유저 새로 접속 시 payout을 재계산해서 브로드캐스트하는 로직 추가 필요
                var session = _gameRoomManager.GetGameSession(joinRequest.RoomId);
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

        private async Task HandleBetting(TcpClient client, BetRequest betRequest)
        {
            // TODO: TotalBetAmount가 MaxBetAmount를 초과할 때는 모든 유저들에게 페이아웃 반환하고 모든 유저의 페이아웃 초기화
            // TODO: 해당 유저의 페이아웃 재계산 기능
            // TODO: TotalBetAmount에 따라 해당 유저의 잭팟 확률을 조정하는 기능
            try
            {
                int roomId = _clientManager.GetUserRoomId(client);
                var userState = _clientManager.GetGameUserState(client);
                var gameState = _gameRoomManager.GetGameSession(roomId);

                if (gameState != null && userState != null)
                {
                    // 유저의 코인 수 변경
                    var updatedUser = await _userTcpService.UpdateUserAsync(userState.UserId, "coins", -betRequest.BetAmount);

                    if (updatedUser != null)
                    { 
                        lock (_sessionLock) // GameSession 업데이트 보호
                        {
                            // 배팅 처리
                            _clientManager.UpdateGameUserState(client, "userTotalBetAmount", betRequest.BetAmount);// 배팅한 게임 유저 상태의 TotalBet을 수정
                            gameState.TotalBetAmount += betRequest.BetAmount;
                            gameState.TotalJackpotAmount += (long)Math.Round(betRequest.BetAmount * 0.1);
                        }

                        // 요청 클라이언트에게 응답 전송
                        var response = new ClientResponse
                        {
                            ResponseType = "BetResponse",
                            BetResponseData = new BetResponse { UpdatedCoins = updatedUser.Coins }
                        };
                        byte[] responseData = ProtobufUtils.SerializeProtobuf(response);
                        var stream = client.GetStream();
                        await stream.WriteAsync(responseData, 0, responseData.Length);
                    }
                    Console.WriteLine($"[socket] Room {roomId}: TotalBet = {gameState.TotalBetAmount}");

                    // 변경된 세션 데이터 브로드캐스트
                    BroadcastGameState(roomId, gameState);

                    // 변경된 게임 유저 상태 브로드캐스트
                    BroadcaseGameUserState(roomId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error handling betting: {ex.Message}");
            }
        }


        // TODO: 클라이언트가 게임에서 승리 시 코인을 더하는 기능 -> 페이아웃은 유저의 보유 금액 당 이익이 10프로를 초과하면 해당 유저는 페이아웃 초기화

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

                // 2. 해당 룸에 연결된 클라이언트 가져오기
                var clientsInRoom = _clientManager.GetClientsInRoom(roomId);

                // 3. 공통 브로드캐스트 메서드 호출
                BroadcastToClients(clientsInRoom, responseData);
            }
        }

        private void BroadcaseGameUserState(int roomId)
        {
            // 2. 해당 룸에 연결된 클라이언트 가져오기
            var clientsInRoom = _clientManager.GetClientsInRoom(roomId);

            foreach (var client in clientsInRoom)
            {
                var responseData = new ClientResponse
                {
                    ResponseType = "GameUserState",
                    GameUserState = _clientManager.GetGameUserState(client)
                };

                // 공통 브로드캐스트 메서드 호출
                BroadcastToClients([ client ], responseData);
            }
        }

        private static void BroadcastToClients(IEnumerable<TcpClient> clients, ClientResponse response)
        {
            try
            {
                // 응답 데이터 직렬화
                byte[] responseData = ProtobufUtils.SerializeProtobuf(response);

                // 클라이언트들에게 데이터 전송
                foreach (var client in clients)
                {
                    try
                    {
                        if (client.Connected)
                        {
                            var stream = client.GetStream();
                            stream.Write(responseData, 0, responseData.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[socket] Error broadcasting to client: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] General broadcast error: {ex.Message}");
            }
        }
    }
}
