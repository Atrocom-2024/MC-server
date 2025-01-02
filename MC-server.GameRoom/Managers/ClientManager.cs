using System.Net.Sockets;
using System.Collections.Concurrent;
using MC_server.GameRoom.Models;

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

        public void AssignClientToRoom(TcpClient client, string userId, int roomId)
        {
            var userState = new GameUserState
            {
                GameUserId = userId,
                RoomId = roomId,
                CurrentPayout = 0.0,
                UserTotalBetAmount = 0,
                JackpotProb = 0.1
            };

            // _clientStates에 동일한 TcpClient 키를 덮어쓸 경우 예기치 않은 동작이 발생할 수 있어 메서드를 통한 추가
            _clientStates.AddOrUpdate(client, userState, (key, existingValue) => userState);
        }

        public GameUserState GetGameUserState(TcpClient client)
        {
            if (_clientStates.TryGetValue(client, out var userState))
            {
                return userState;
            }

            throw new InvalidOperationException("Client not found or not assigned to any room.");
        }

        public int GetRoomId(TcpClient client)
        {
            if (_clientStates.TryGetValue(client, out var userState))
            {
                return userState.RoomId;
            }

            throw new InvalidOperationException("Client not assigned to any room.");
        }

        public IEnumerable<TcpClient> GetClientsInRoom(int roomId)
        {
            return _clientStates.Where(pair => pair.Value.RoomId == roomId).Select(pair => pair.Key);
        }
    }
}
