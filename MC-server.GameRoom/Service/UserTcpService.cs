﻿using MC_server.Core.Models;
using MC_server.Core.Services;

namespace MC_server.GameRoom.Service
{
    public class UserTcpService
    {
        private readonly UserService _userService;

        public UserTcpService(UserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public async Task<User?> UpdateUserAsync(string userId, string property, object value)
        {
            // 유저 정보 가져오기
            User? user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return null;
            }

            switch (property)
            {
                case "coins":
                    // 코인 업데이트
                    if (value is int coinAmount)
                    {
                        user.Coins += coinAmount;
                        Console.WriteLine($"[socket] Updated coins for user to {user.Coins}");
                    }
                    else
                    {
                        throw new ArgumentException("Invalid value type for coins.");
                    }
                    break;
                default:
                    throw new ArgumentException($"Property '{property}' is not a valid UserUpdate property.");
            }

            // 변경 사항 저장
            await _userService.UpdateUserAsync(user);
            return user;
        }
    }
}