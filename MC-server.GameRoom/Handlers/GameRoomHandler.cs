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

        private readonly UserTcpService _userTcpService;

        // GameSession에 대한 동기화 제어를 위해 사용됨 -> 다수의 스레드가 동시에 GameSession을 읽거나 수정하려고 할 때 충돌을 방지
        private readonly object _sessionLock = new object();

        public GameRoomHandler(GameRoomManager gameRoomManager, ClientManager clientManager, UserTcpService userTcpService)
        {
            _gameRoomManager = gameRoomManager ?? throw new ArgumentNullException(nameof(gameRoomManager));
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _userTcpService = userTcpService ?? throw new ArgumentNullException(nameof(userTcpService));
        }

        public async Task HandleGameRoomAsync(TcpClient client)
        {
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
                                    await HandleBetting(client, request.BetData);
                                }
                                break;
                            case "AddCoinsRequest":
                                if (request.AddCoinsData != null)
                                {
                                    await HandleAddCoins(client, request.AddCoinsData);
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
            try
            {
                Console.WriteLine($"Join User ID: {joinRequest.UserId}");

                // 1. 유저가 해당 룸에 Join 시 해당 룸에 유저 정보 등록
                _clientManager.AddClient(client, joinRequest.UserId, joinRequest.RoomId);

                // 2. 유저가 해당 룸에 Join 할 때마다 해당 게임의 TotalUser + 1
                _gameRoomManager.IncrementTotalUser(joinRequest.RoomId);

                // 3. 해당 룸에 연결된 클라이언트와 게임 세션 가져오기
                var clientsInRoom = _clientManager.GetClientsInRoom(joinRequest.RoomId);
                var gameSession = _gameRoomManager.GetGameSession(joinRequest.RoomId);

                // 4. 해당 룸에 접속 중인 클라이언트들의 payout 변경
                foreach (var gameUserClient in clientsInRoom)
                {
                    var gameUser = _clientManager.GetGameUser(gameUserClient);
                    _clientManager.UpdateGameUser(client, "currentPayout", GameUserStateUtils.CalculatePayout(gameUser, gameSession));
                }

                // 5. 유저 상태 브로드캐스트
                BroadcaseGameUserState(joinRequest.RoomId);

                Console.WriteLine($"Jackpot amount is {gameSession.TotalJackpotAmount}");
                // 6. 게임 상태 브로드캐스트
                var gameState = new GameState
                {
                    TotalJackpotAmount = gameSession.TotalJackpotAmount,
                    IsJackpot = gameSession.IsJackpot
                };

                BroadcastGameState(joinRequest.RoomId, gameState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error handling room join: {ex.Message}");
            }
        }

        private async Task HandleBetting(TcpClient client, BetRequest betRequest)
        {
            // TODO: TotalBetAmount가 MaxBetAmount를 초과할 때는 모든 유저들에게 페이아웃 반환하고 모든 유저의 페이아웃 초기화
            // TODO: TotalBetAmount에 따라 해당 유저의 잭팟 확률을 조정하는 기능
            try
            {
                int roomId = _clientManager.GetUserRoomId(client);
                var gameUser = _clientManager.GetGameUser(client);
                var gameSession = _gameRoomManager.GetGameSession(roomId);
                
                // 유저의 코인 수 변경
                var updatedUser = await _userTcpService.UpdateUserAsync(gameUser.UserId, "coins", -betRequest.BetAmount);

                if (updatedUser != null)
                { 
                    lock (_sessionLock) // GameSession 업데이트 보호
                    {
                        // 배팅 처리
                        var newPayout = GameUserStateUtils.CalculatePayout(gameUser, gameSession);
                        Console.WriteLine($"payout 재계산됨 {newPayout}");
                        _clientManager.UpdateGameUser(client, "currentPayout", newPayout); // 해당 유저의 페이아웃 재계산
                        _clientManager.UpdateGameUser(client, "userTotalBetAmount", betRequest.BetAmount);// 배팅한 게임 유저의 총 배팅 금액을 수정
                        gameSession.TotalBetAmount += betRequest.BetAmount; // 해당 룸의 총 배팅 금액 변경
                        gameSession.TotalJackpotAmount += (long)Math.Round(betRequest.BetAmount * 0.1); // 배팅 금액의 10%만큼 잭팟 금액에 누적
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
                Console.WriteLine($"[socket] Room {roomId}: TotalBet = {gameSession.TotalBetAmount}");

                var gameState = new GameState
                {
                    TotalJackpotAmount = gameSession.TotalJackpotAmount,
                    IsJackpot = gameSession.IsJackpot
                };

                // 변경된 세션 데이터 브로드캐스트
                BroadcastGameState(roomId, gameState);

                // 변경된 게임 유저 상태 브로드캐스트
                BroadcaseGameUserState(roomId);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[socket] Error handling betting: {ex.Message}");
            }
        }

        public async Task HandleAddCoins(TcpClient client, AddCoinsRequest addCoinsRequest)
        {
            // TODO: 유저가 받은 이익이 유저 자본의 10% 이상이 되면 Payout 초기화
            try
            {
                // 유저의 코인 수 변경
                var updatedUser = await _userTcpService.UpdateUserAsync(addCoinsRequest.UserId, "coins", addCoinsRequest.AddCoinsAmount);

                if (updatedUser != null)
                {
                    lock (_sessionLock)
                    {
                        // 코인 추가 처리
                        _clientManager.UpdateGameUser(client, "userTotalProfit", addCoinsRequest.AddCoinsAmount);
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


        private void BroadcastGameState(int roomId, GameState gameState)
        {
            lock ( _sessionLock) // GameSession 읽기 보호
            {
                // 1. 응답 데이터 생성
                var responseData = new ClientResponse
                {
                    ResponseType = "GameState",
                    GameState = gameState
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
