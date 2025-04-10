namespace MC_server.API.DTOs.DailySpin
{
    public class DailySpinExecutionRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int SpinRewardCoins { get; set; }
    }
}
