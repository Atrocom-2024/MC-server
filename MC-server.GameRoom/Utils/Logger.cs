namespace MC_server.GameRoom.Utils
{
    public class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine($"[socket] [{DateTime.UtcNow:O}] {message}");
        }
    }
}
