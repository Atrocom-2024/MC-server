using MC_server.GameRoom.Managers.Models;
using MC_server.Core.Models;

namespace MC_server.GameRoom.Utils
{
    public class GameSessionUtils
    {
        // 인스턴스를 메서드 내에서 참조하지 않기 때문에 정적 메서드로 선언
        public static GameSession CreateNewSession(Room room)
        {
            return new GameSession
            {
                GameId = PublicUtils.GenerateRandomString(20),
                TotalBetAmount = 0,
                TotalUser = 0,
                IsJackpot = false,
                TargetPayout = room.TargetPayout,
                MaxBetAmount = room.MaxBetAmount,
                MaxUser = room.MaxUser,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
