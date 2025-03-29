using MC_server.API.DTOs.User;
using MC_server.API.Utils;
using MC_server.Core.Models;
using MC_server.Core.Services;
using System.ComponentModel.DataAnnotations;

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

        public async Task<User?> GetUserDetailsForApiAsync(string userId)
        {
            // Core 서비스 호출
            var user = await _userService.GetUserByIdAsync(userId);

            // API에 특화된 데이터 반환
            //return new { user.UserId, user.Nickname, user.Level, user.Coins };
            return user;
        }

        public async Task<User> CreateUserAsync(string userId, string provider)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(provider))
            {
                throw new ValidationException("UserId and Provider are required.");
            }

            // 유저 중복 검증
            if (await _userService.GetUserByIdAsync(userId) != null)
            {
                throw new InvalidOperationException($"User with ID '{userId}' already exists.");
            }

            // 유저 생성
            var user = new User
            {
                UserId = userId,
                Provider = provider,
                Nickname = UserUtility.GenerateRandomNickname(), // 랜덤 닉네임 생성
                Coins = 500000, // 기본 코인
                Level = 1, // 기본 레벨
                Experience = 0, // 초기 경험치
            };

            return await _userService.CreateUserAsync(user);
        }

        public async Task<object?> UpdateUserAsync(string userId, UserUpdateRequest request)
        {
            // 유저 정보 가져오기
            User user = await GetUserDetailsForApiAsync(userId) ?? throw new KeyNotFoundException($"User with ID '{userId}' not found.");

            var updatedFields = new Dictionary<string, object>();

            // 닉네임 업데이트
            if (!string.IsNullOrWhiteSpace(request.Nickname))
            {
                // 닉네임 중복 확인
                if (await _userService.IsNicknameTakenAsync(request.Nickname))
                {
                    throw new InvalidOperationException("Nickname is already taken");
                }

                user.Nickname = request.Nickname;
                updatedFields["nickname"] = request.Nickname;
            }

            // 코인 업데이트
            if (request.AddCoins.HasValue)
            {
                user.Coins += request.AddCoins.Value;
                updatedFields["addCoins"] = user.Coins;
            }

            // 레벨 업데이트
            if (request.Level.HasValue)
            {
                user.Level = request.Level.Value;
                updatedFields["level"] = request.Level.Value;
            }

            // 경험치 업데이트
            if (request.Experience.HasValue)
            {
                user.Experience = request.Experience.Value;
                updatedFields["experience"] = request.Experience.Value;
            }

            // 변경 사항 저장
            await _userService.UpdateUserAsync(user);

            return updatedFields;
        }
    }
}
