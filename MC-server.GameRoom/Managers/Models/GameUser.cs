﻿namespace MC_server.GameRoom.Managers.Models
{
    public class GameUser
    {
        public string UserId { get; set; } = string.Empty;

        public int RoomId { get; set; }

        public decimal CurrentPayout { get; set; }

        public int UserTotalProfit { get; set; }

        public long UserTotalBetAmount { get; set; }

        public decimal JackpotProb { get; set; }
    }
}