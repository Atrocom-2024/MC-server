using MC_server.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MC_server.Core.Services
{
    public class GameService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GameService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        // 게임 생성
        public async Task<GameRecord> CreateGameRecordAsync(GameRecord gameRecord)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 데이터 검증
            if (await dbContext.Games.AnyAsync(g => g.RoomId == gameRecord.RoomId))
            {
                throw new InvalidOperationException($"Game with ID '{gameRecord.RoomId}'");
            }

            dbContext.Games.Add(gameRecord);
            await dbContext.SaveChangesAsync();
            return gameRecord;
        }

        // 게임 정보 읽기
        public async Task<GameRecord?> GetGameRecordByIdAsync(int roomId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            GameRecord? game = await dbContext.Games.FindAsync(roomId);

            if (game == null)
            {
                return null;
            }

            return game;
        }

        public async Task<List<GameRecord>> GetAllGamesAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.Games.ToListAsync();
        }

        public async Task<GameRecord> UpdateGameRecordAsync(GameRecord gameRecord)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            dbContext.Games.Update(gameRecord);
            await dbContext.SaveChangesAsync();
            return gameRecord;
        }

        public async Task DeleteGameAsymc(int roomId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                GameRecord? game = await GetGameRecordByIdAsync(roomId);
                if (game != null)
                {
                    dbContext.Games.Remove(game);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteGameAsync: {ex.Message}");
            }
        }
    }
}
