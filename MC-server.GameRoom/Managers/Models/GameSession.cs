namespace MC_server.GameRoom.Managers.Models
{
    public class GameSession
    {
        public string GameId { get; set; } = string.Empty;

        public long TotalBetAmount { get; set; }

        public int TotalUser { get; set; }

        public bool IsJackpot { get; set; }

        public decimal TargetPayout { get; set; }

        public long MaxBetAmount { get; set; }

        public int MaxUser { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
