﻿using MC_server.Core.Models;
using MC_server.GameRoom.Models;
using System.Collections.Concurrent;

namespace MC_server.GameRoom.Utils
{
    public class GameSessionUtils
    {
        // 인스턴스를 메서드 내에서 참조하지 않기 때문에 정적 메서드로 선언
        public static GameSession CreateNewSession(Room room)
        {
            return new GameSession
            {
                TotalBetAmount = 0,
                TotalUser = 0,
                TotalJackpotAmount = 0,
                IsJackpot = false,
                TargetPayout = room.TargetPayout,
                MaxBetAmount = room.MaxBetAmount,
                MaxUser = room.MaxUser,
                BaseJackpotAmount = room.BaseJackpotAmount,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}