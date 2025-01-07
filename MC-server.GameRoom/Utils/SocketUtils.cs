using System.Net.Sockets;

namespace MC_server.GameRoom.Utils
{
    public static class SocketUtils
    {
        public static bool IsSocketConnected(Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
