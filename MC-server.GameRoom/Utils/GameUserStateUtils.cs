using MC_server.GameRoom.Managers.Models;

namespace MC_server.GameRoom.Utils
{
    public static class GameUserStateUtils
    {
        public static decimal CalculatePayout(GameUser gameUser, GameSession gameSession)
        {
            Console.WriteLine($"현재 {gameUser.RoomId}번 방 접속 유저 수: {gameSession.TotalUser}");
            var adjustedProb = ((gameSession.TargetPayout - gameUser.CurrentPayout) / 2);
            var part_A = (adjustedProb * (gameUser.UserTotalBetAmount / gameSession.MaxBetAmount) + adjustedProb * (gameSession.TotalUser / gameSession.MaxUser));

            return part_A;
        }
    }
}
