using MC_server.API.DTOs.User;
using MC_server.API.Utils;
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

        public async Task<object?> GetUserDetailsForApiAsync(string userId)
        {
            //try
            //{
                // Core 서비스 호출
                User? user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return null;
            }

                // API에 특화된 데이터 반환
                return new { user.UserId, user.Nickname, user.Level, user.Coins };
            //}
            //catch (KeyNotFoundException ex)
            //{

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error in GetUserDetailsForApiAsync: {ex.Message}");
            //    return new { Error = "An unexpected error occurred.", ex.Message };
            //}
        }

        public async Task<object> CreateUserAsync(UserCreateRequest request)
        {
            // provider가 google일 경우 추가 데이터 검증
            //if (provider.ToLower() == "google" && (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name)))
            //{
            //    throw new ArgumentException("For Google provider, email and name are required");
            //}

            // 유저 중복 검증
            if (await _userService.GetUserByIdAsync(request.UserId) != null)
            {
                throw new InvalidOperationException($"User with ID '{request.UserId}' already exists.");
            }

            //if (request.Provider == "google" && request.DeviceId != null)
            //{
            //    User? googleUser = await _userService.GetUserByIdAsync(request.DeviceId);

            //    if (googleUser != null)
            //    {
            //        googleUser.UserId = request.UserId;
            //        googleUser.Provider = request.Provider;

            //        // 변경 사항 저장
            //        return await _userService.UpdateUserAsync(googleUser);
            //    }
            //}

            // 유저 생성
            User user = new User
            {
                UserId = request.UserId,
                Provider = request.Provider,
                Email = request.Email, // google일 경우 추가, 다른 provider면 null
                Name = request.Name, // google일 경우 추가, 다른 provider면 null
                Nickname = UserUtility.GenerateRandomNickname(), // 랜덤 닉네임 생성
                Coins = 1000000, // 기본 코인
                Level = 1, // 기본 레벨
                Experience = 0, // 초기 경험치
            };

            return await _userService.CreateUserAsync(user);
        }

        public async Task<object?> UpdateUserAsync(string userId, UserUpdateRequest request)
        {
            // 유저 정보 가져오기
            try
            {
                User? user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    return null;
                }

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateUserAsync: {ex.Message}");
                return new { Error = "An unexpected error occurred.", ex.Message };
            }
        }
    }
}
