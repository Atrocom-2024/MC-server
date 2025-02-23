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
            var tokenResponse = await _googleAuthService.ExchangeAuthCodeForTokenAsync(request.UserId, request.AuthCode);
            Console.WriteLine(tokenResponse.AccessToken);

            // 2. Access Token 검증
            var tokenInfo = await _googleAuthService.ValidationAccessToken(tokenResponse.AccessToken);
            Console.WriteLine($"User id: {tokenInfo.UserId}");
            Console.WriteLine($"User email: {tokenInfo.Email}");

            return Ok(tokenInfo);
        }
    }
}
