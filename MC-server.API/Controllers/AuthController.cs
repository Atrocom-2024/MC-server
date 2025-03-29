using MC_server.API.DTOs.Auth;
using MC_server.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MC_server.API.Controllers
{
    [ApiController]
    [Route("/api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly GoogleAuthService _googleAuthService;
        private readonly UserApiService _userApiService;

        public AuthController(GoogleAuthService googleAuthService, UserApiService userApiService)
        {
            _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService));
            _userApiService = userApiService ?? throw new ArgumentNullException(nameof(userApiService));
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.AuthCode))
            {
                throw new ValidationException("AuthCode is required.");
            }

            // 1. Google Auth Code 검증 (토큰 교환)
            var tokenResponse = await _googleAuthService.ExchangeAuthCodeForTokenAsync(request.AuthCode);

            // 2. Access Token 검증
            var isValidated = await _googleAuthService.ValidationAccessToken(tokenResponse.AccessToken);

            if (!isValidated)
            {
                throw new UnauthorizedAccessException("Access token is invalid or expired.");
            }

            // 3. 사용자 정보 가져오기 -> 유저 정보가 없다면 자동 회원가입
            var userInfo = await _userApiService.GetUserDetailsForApiAsync(request.UserId)
                ?? await _userApiService.CreateUserAsync(request.UserId, "google");

            return Ok(userInfo);
        }

        [HttpPost("guest")]
        public async Task<IActionResult> GuestAuth([FromBody] GuestAuthRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.UserId))
            {
                throw new ValidationException("UserId is required.");
            }
            // 사용자 정보 가져오기 -> 유저 정보가 없다면 자동 회원가입
            var userInfo = await _userApiService.GetUserDetailsForApiAsync(request.UserId)
                ?? await _userApiService.CreateUserAsync(request.UserId, "guest");

            return Ok(userInfo);
        }
    }
}
