using MC_server.Core;
using MC_server.Core.Models;
using MC_server.Core.Services;
using MC_server.GameRoom.Managers.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MC_server.GameRoom.Service
{
    public class GameTcpService
    {
        private readonly RoomService _roomService;
        private readonly GameService _gameService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GameTcpService(RoomService roomService, GameService gameService, IServiceScopeFactory serviceScopeFactory)
        {
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
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
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var existingGameRecord = await dbContext.FindAsync<GameRecord>(roomId) ?? throw new Exception("GameRecord not found");

                existingGameRecord.TotalBetAmount = gameSession.TotalBetAmount;
                existingGameRecord.TotalUser = gameSession.TotalUser;
                existingGameRecord.IsJackpot = gameSession.IsJackpot;

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RecordGameResult: {ex.Message}");
                Console.WriteLine($"{ex.InnerException?.Message}");
            }
        }
    }
}
