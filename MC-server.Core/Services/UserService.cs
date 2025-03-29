using MC_server.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MC_server.Core.Services
{
    // ApplicationDbContext 생성자에서 주입받아 사용하면 DbContext가 스코프 외부로 확장될 가능성이 있음
    // IServiceScopeFactory를 사용해 DbContext의 생명주기를 관리 -> 멀티 스레드 환경에서 안전
    // API나 다른 프로젝트에서 호출해 사용할 수 있는 공통 로직을 포함
    // 독립적인 로직 구현에 집중하며, HTTP 요청/응답과 같은 API 세부 사항을 포함하지 않음
    // 공통적으로 사용되는 기능(예: 유저 검증, 데이터 변환 등)
    public class UserService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public UserService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        // 유저 생성 - test4
        public async Task<User> CreateUserAsync(User user)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 데이터 검증
            if (await dbContext.Users.AnyAsync(u => u.UserId == user.UserId))
            {
                throw new InvalidOperationException($"User with ID '{user.UserId}'.");
            }

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            return user;
        }

        // 유저 정보 읽기
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            User? user = await dbContext.Users.FindAsync(userId);

            return user;
        }

        // 유저 정보 수정
        public async Task<User> UpdateUserAsync(User user)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync();
            return user;
        }

        // 유저 정보 제거
        public async Task DeleteUserAsync(string userId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            User? user = await GetUserByIdAsync(userId);

            if (user != null)
            {
                dbContext.Users.Remove(user);
                await dbContext.SaveChangesAsync();
            }
        }

        // 유저 닉네임 중복 확인
        public async Task<bool> IsNicknameTakenAsync(string nickname)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.Users.AnyAsync(u => u.Nickname == nickname);
        }
    }
}
