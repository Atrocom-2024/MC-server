using MC_server.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MC_server.Core.Services
{
    public class DailySpinService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DailySpinService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        /// <summary>
        /// 데일리 스핀 레코드 생성
        /// </summary>
        /// <param name="dailySpin"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<DailySpin> CreateDailySpinAsync(DailySpin dailySpin)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 데이터 검증
            if (await dbContext.DailySpins.AnyAsync(d => d.UserId == dailySpin.UserId))
            {
                throw new InvalidOperationException($"DailySpin with User ID '{dailySpin.UserId}' already exists");
            }

            dbContext.DailySpins.Add(dailySpin);
            await dbContext.SaveChangesAsync();
            return dailySpin;
        }

        /// <summary>
        /// 유저 Id로 데일리 스핀 레코드 조회
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<DailySpin?> GetDailySpinByUserIdAsync(string userId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            DailySpin? dailySpin = await dbContext.DailySpins.FindAsync(userId);

            if (dailySpin == null)
            {
                return null;
            }

            return dailySpin;
        }

        /// <summary>
        /// 데일리 스핀 레코드 수정
        /// </summary>
        /// <param name="dailySpin"></param>
        /// <returns></returns>
        public async Task<DailySpin?> UpdateDailySpinAsync(DailySpin dailySpin)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            dbContext.DailySpins.Update(dailySpin);
            await dbContext.SaveChangesAsync();
            return dailySpin;
        }
    }
}
