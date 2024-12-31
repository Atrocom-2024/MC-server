using System.Net.Sockets;
using System.Collections.Concurrent;

namespace MC_server.GameRoom.Managers
{
    public class ClientManager
    {
        // 현재 연결된 모든 클라이언트를 관리하는 리스트
        private static readonly List<TcpClient> _connectedClients = new List<TcpClient>();

        // 각 클라이언트가 어느 룸에 연결되어 있는지 추적 -> 키는 클라이언트 객체, 값은 해당 클라이언트가 속한 룸의 id
        private readonly ConcurrentDictionary<TcpClient, int> _clientRoomMapping = new ConcurrentDictionary<TcpClient, int>();

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

            _clientRoomMapping.TryRemove(client, out _);
        }

        public void AssignClientToRoom(TcpClient client, int roomId)
        {
            _clientRoomMapping[client] = roomId;
        }

        public int GetRoomId(TcpClient client)
        {
            if (_clientRoomMapping.TryGetValue(client, out int roomId))
            {
                return roomId;
            }

            throw new InvalidOperationException("Client not assigned to any room.");
        }

        public IEnumerable<TcpClient> GetClientsInRoom(int roomId)
        {
            return _clientRoomMapping.Where(pair => pair.Value == roomId).Select(pair => pair.Key);
        }
    }
}
