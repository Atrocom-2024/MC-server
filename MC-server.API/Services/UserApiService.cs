using MC_server.Core.Models;
using MC_server.Core.Services;

namespace MC_server.API.Services
{
    // API 요청에 특화된 로직 구현
    // 일반적으로 Core의 Services를 호출해 필요한 데이터를 가져오거나 처리 결과를 반환
    // HTTP 요청/응답, 클라이언트와의 통신 관련 로직에 특화
    // API 요청에 맞게 데이터 필터링, 변환, 포맷팅
    public class UserApiService
    {
        private readonly UserService _userService;

        public UserApiService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<object> GetUserDetailsForApiAsync(string userId)
        {
            // Core 서비스 호출
            User user = await _userService.GetUserByIdAsync(userId);

            // API에 특화된 데이터 반환
            return new { user.UserId, user.Nickname, user.Level, user.Coins };
        }
    }
}
