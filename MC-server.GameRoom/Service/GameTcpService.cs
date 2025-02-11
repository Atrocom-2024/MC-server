using MC_server.Core.Models;
using MC_server.Core.Services;
using MC_server.GameRoom.Managers.Models;

namespace MC_server.GameRoom.Service
{
    public class GameTcpService
    {
        private readonly RoomService _roomService;
        private readonly GameService _gameService;

        public GameTcpService(RoomService roomService, GameService gameService)
        {
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        }

        public async Task<List<Room>> GetAllRoomsAsync()
        {
            var allRooms = await _roomService.GetAllRoomsAsync();

            return allRooms;
        }

        public async Task<Room?> GetRoomByIdAsync(int roomId)
        {
            return await _roomService.GetRoomByIdAsync(roomId);
        }

        public async Task<GameRecord?> GetGameRecordByIdAsync(int roomId)
        {
            return await _gameService.GetGameRecordByIdAsync(roomId);
        }

        public async Task RecordGameResult(int roomId, GameSession gameSession)
        {
            try
            {
                var GameRecordData = new GameRecord
                {
                    RoomId = roomId,
                    TotalBetAmount = gameSession.TotalBetAmount,
                    TotalUser = gameSession.TotalUser,
                    TotalJackpotAmount = gameSession.TotalJackpotAmount,
                    IsJackpot = gameSession.IsJackpot,
                };

                await _gameService.UpdateGameRecordAsync(GameRecordData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RecordGameResult: {ex.Message}");
                Console.WriteLine($"{ex.InnerException?.Message}");
            }
        }
    }
}
