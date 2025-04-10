using MC_server.API.DTOs.DailySpin;
using MC_server.API.Services;
using MC_server.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MC_server.API.Controllers
{
    [ApiController]
    [Route("api/daily-spins")]
    public class DailySpinController: ControllerBase
    {
        private readonly DailySpinApiService _dailySpinApiService;

        private const int CooldownSeconds = 24 * 60 * 60; // 스핀 쿨다운 시간: 24시간 = 86400초

        public DailySpinController(DailySpinApiService dailySpinApiService)
        {
            _dailySpinApiService = dailySpinApiService ?? throw new ArgumentNullException(nameof(dailySpinApiService));
        }

        /// <summary>
        /// 유저의 데일리 스핀 정보 응답
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetDailySpin([FromRoute] string userId)
        {
            Console.WriteLine("[web] 데일리 스핀 정보 요청 발생");
            var dailySpinInfo = await _dailySpinApiService.GetDailySpinInfoAsync(userId);
            var response = new DailySpinStatusResponse();

            // 데일리 스핀 정보가 없는 경우
            if (dailySpinInfo == null)
            {
                // 첫 스핀이므로 스핀 가능으로 간주
                response.IsAvailable = true;
                response.RemainingSeconds = 0;
            }
            else
            {
                // 스핀 가능 여부와 남은 시간 계산
                DateTime lastSpinTime = dailySpinInfo.LastSpinTime;
                DateTime currentTime = DateTime.UtcNow;
                DateTime nextSpinTime = lastSpinTime.AddSeconds(CooldownSeconds);

                // 남은 초가 0 이하이면 스핀 가능
                int remainingSeconds = (int)Math.Max(0, Math.Ceiling((nextSpinTime - currentTime).TotalSeconds));
                response.IsAvailable = remainingSeconds == 0;
                response.RemainingSeconds = remainingSeconds;
            }

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> ExecutionDailySpin([FromBody] DailySpinExecutionRequest request)
        {
            Console.WriteLine("[web] 데일리 스핀 발생");
            var response = new DailySpinExecutionResponse();
            var processedUserInfo = await _dailySpinApiService.ProcessExcutionDailySpinAsync(request.UserId, request.SpinRewardCoins);

            response.Message = "successful processed daily spin";
            response.ProcessedCoins = processedUserInfo.Coins;

            return Ok(response);
        }

        public class DailySpinStatusResponse
        {
            public bool IsAvailable { get; set; }
            public int RemainingSeconds { get; set; }
        }

        public class DailySpinExecutionResponse
        {
            public string Message { get; set; } = string.Empty;
            public long ProcessedCoins { get; set; }
        }
    }
}
