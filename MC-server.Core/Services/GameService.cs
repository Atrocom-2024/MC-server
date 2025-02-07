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
        public async Task<GameRecord> CreateGameRecordAsync(GameRecord game)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 데이터 검증
            if (await dbContext.Games.AnyAsync(g => g.GameId == game.GameId))
            {
                throw new InvalidOperationException($"Game with ID '{game.GameId}'");
            }

            dbContext.Games.Add(game);
            await dbContext.SaveChangesAsync();
            return game;
        }

        // 게임 정보 읽기
        public async Task<GameRecord?> GetGameRecordByIdAsync(string gameId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            GameRecord? game = await dbContext.Games.FindAsync(gameId);

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

        public async Task DeleteGameAsymc(string gameId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                GameRecord? game = await GetGameRecordByIdAsync(gameId);
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
