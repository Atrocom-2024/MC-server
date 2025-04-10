using MC_server.Core.Models;
using MC_server.Core.Services;

namespace MC_server.API.Services
{
    public class DailySpinApiService
    {
        private readonly DailySpinService _dailySpinService;
        private readonly UserService _userService;

        public DailySpinApiService(DailySpinService dailySpinService, UserService userService)
        {
            _dailySpinService = dailySpinService ?? throw new ArgumentNullException(nameof(dailySpinService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// 유저의 데일리 스핀 정보 조회
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<DailySpin?> GetDailySpinInfoAsync(string userId)
        {
            var userDailySpin = await _dailySpinService.GetDailySpinByUserIdAsync(userId);
            return userDailySpin;
        }

        public async Task<User> ProcessExcutionDailySpinAsync(string userId, int spinRewardCoins)
        {
            // 1. 입력 값 검증
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            // 2. 기존 데일리 스핀 레코드 조회
            var userDailySpin = await _dailySpinService.GetDailySpinByUserIdAsync(userId);

            // 3. 레코드가 없는 경우 새로 생성하고 레코드가 있는 경우 마지막 스핀 시간 갱신
            if (userDailySpin == null)
            {
                userDailySpin = new DailySpin
                {
                    UserId = userId,
                    LastSpinTime = DateTime.UtcNow
                };

                await _dailySpinService.CreateDailySpinAsync(userDailySpin);
            }
            else
            {
                userDailySpin.LastSpinTime = DateTime.UtcNow;

                await _dailySpinService.UpdateDailySpinAsync(userDailySpin);
            }

            // 4. 유저의 코인 수 변경
            User? user = await _userService.GetUserByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User with Id '{userId}' not found.");
            user.Coins += spinRewardCoins;

            // 5. 업데이트된 유저 코인 반환
            return user;
        }
    }
}
