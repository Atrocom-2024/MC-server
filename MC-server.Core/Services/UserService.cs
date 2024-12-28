using System;
using MC_server.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MC_server.Core.Services
{
    // API나 다른 프로젝트에서 호출해 사용할 수 있는 공통 로직을 포함
    // 독립적인 로직 구현에 집중하며, HTTP 요청/응답과 같은 API 세부 사항을 포함하지 않음
    // 공통적으로 사용되는 기능(예: 유저 검증, 데이터 변환 등)
    public class UserService
    {
        private readonly ApplicationDbContext _dbContext;

        public UserService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 유저 생성 - test4
        public async Task<User> CreateUserAsync(User user)
        {
            // 데이터 검증
            if (await _dbContext.Users.AnyAsync(u => u.UserId == user.UserId))
            {
                throw new InvalidOperationException($"User with ID '{user.UserId}'.");
            }

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        // 유저 정보 읽기
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            User? user = await _dbContext.Users.FindAsync(userId);

            return user;
        }

        // 유저 정보 수정
        public async Task<User> UpdateUserAsync(User user)
        {
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        // 유저 정보 제거
        public async Task DeleteUserAsync(string userId)
        {
            User? user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        // 유저 닉네임 중복 확인
        public async Task<bool> IsNicknameTakenAsync(string nickname)
        {
            return await _dbContext.Users.AnyAsync(u => u.Nickname == nickname);
        }
    }
}
