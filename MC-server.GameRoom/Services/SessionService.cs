namespace MC_server.GameRoom.Services
{
    public class SessionService
    {
        private readonly GameRoomService _gameRoomService;

        public SessionService(GameRoomService gameRoomService)
        { 
            _gameRoomService = gameRoomService;
        }

        public void StartSessionTimers()
        {
            foreach (var roomId in _gameRoomService.GetAllSessions().Keys)
            {
                Timer timer = new Timer(_ =>
                {
                    _gameRoomService.ResetSession(roomId);
                    Console.WriteLine($"[socket] Room {roomId}: New session started.");
                }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
        }
    }
}
