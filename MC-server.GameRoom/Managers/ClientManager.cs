using System.Net.Sockets;
using System.Collections.Concurrent;

using MC_server.GameRoom.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace MC_server.GameRoom.Managers
{
    public class ClientManager
    {
        // 현재 연결된 모든 클라이언트를 관리하는 리스트
        private static readonly List<TcpClient> _connectedClients = new List<TcpClient>();

        // 각 클라이언트가 어느 룸에 연결되어 있는지 추적 -> 키는 클라이언트 객체, 값은 해당 클라이언트가 속한 룸의 id
        private readonly ConcurrentDictionary<TcpClient, GameUserState> _clientStates = new ConcurrentDictionary<TcpClient, GameUserState>();

        public void AddClient(TcpClient client)
        {
            lock (_connectedClients)
            {
                _connectedClients.Add(client);
            }
        }

        public void RemoveClient(TcpClient client)
        {
            lock (_connectedClients)
            {
                _connectedClients.Remove(client);
            }

            _clientStates.TryRemove(client, out _);
        }

        public void AssignClientToGameRoom(TcpClient client, string userId, int roomId)
        {
            var userState = new GameUserState
            {
                GameUserId = userId,
                RoomId = roomId,
                CurrentPayout = 0.0M,
                UserTotalBetAmount = 0,
                JackpotProb = 0.1M
            };

            // _clientStates에 동일한 TcpClient 키를 덮어쓸 경우 예기치 않은 동작이 발생할 수 있어 메서드를 통한 추가
            _clientStates.AddOrUpdate(client, userState, (key, existingValue) => userState);
        }

        public void UpdateGameUserState(TcpClient client, string property, object value)
        {
            if (_clientStates.TryGetValue(client, out var userState))
            {
                switch (property)
                {
                    case "currentPayout":
                        if (value is decimal newPayout)
                        {
                            userState.CurrentPayout = newPayout;
                            Console.WriteLine($"[socket] Updated CurrentPayout for user to {userState.CurrentPayout}");
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for CurrentPayout.");
                        }
                        break;
                    case "userTotalBetAmount":
                        if (value is long betAmount)
                        {
                            userState.UserTotalBetAmount += betAmount;
                            Console.WriteLine($"[socket] Updated UserTotalBetAmount for user to {userState.UserTotalBetAmount}");
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for UserTotalBetAmount");
                        }
                        break;
                    case "jackpotProb":
                        if (value is decimal newJackpotProb)
                        {
                            userState.JackpotProb = newJackpotProb;
                            Console.WriteLine($"[socket] Updated JackpotProb for user to {userState.JackpotProb}");
                        }
                        else
                        {
                            throw new ArgumentException("Invalid value type for JackpotProb");
                        }
                        break;
                    default:
                        throw new ArgumentException($"Property '{property}' is not a valid GameUserState property.");
                }
            }
            else
            {
                throw new InvalidOperationException("Client not found or not assigned to any room.");
            }
        }

        // 특정 클라이언트의 정보를 반환하는 메서드
        public GameUserState GetGameUserState(TcpClient client)
        {
            if (_clientStates.TryGetValue(client, out var userState))
            {
                return userState;
            }

            throw new InvalidOperationException("Client not found or not assigned to any room.");
        }

        // 특정 클라이언트가 어떤 룸에 참여 중인지 반환하는 메서드
        public int GetUserRoomId(TcpClient client)
        {
            if (_clientStates.TryGetValue(client, out var userState))
            {
                return userState.RoomId;
            }

            throw new InvalidOperationException("Client not assigned to any room.");
        }

        // 특정 룸에 연결된 클라이언트 정보를 반환하는 메서드
        public IEnumerable<TcpClient> GetClientsInRoom(int roomId)
        {
            return _clientStates.Where(pair => pair.Value.RoomId == roomId).Select(pair => pair.Key);
        }
        
        public ConcurrentDictionary<TcpClient, GameUserState> GetAllClients()
        {
            return _clientStates;
        }
    }
}
