using MC_server.GameRoom.Managers.Models;

namespace MC_server.GameRoom.Utils
{
    public static class GameUserStateUtils
    {
        public static decimal CalculatePayout(GameUser gameUser, GameSession gameSession)
        {
            var adjustedProb = (gameSession.TargetPayout - gameUser.CurrentPayout) / 2;
            var part_A = adjustedProb * ((decimal)gameUser.UserTotalBetAmount / gameSession.MaxBetAmount) + adjustedProb * ((decimal)gameSession.TotalUser / gameSession.MaxUser);

            return part_A / 2;
        }

        public static decimal CalculateJackpotProb(GameUser gameUser)
        {
            return (decimal)Math.Pow((double)gameUser.BetCount / 300, 10) + 0.01M * ((decimal)gameUser.BetCount / 300);
        }
    }
}
