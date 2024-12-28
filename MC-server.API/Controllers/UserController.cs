using MC_server.API.DTOs.User;
using MC_server.API.Services;
using MC_server.Core.Models;
using MC_server.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MC_server.API.Controllers
{
    [Route("/api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly UserApiService _userApiService;

        public UserController(UserService userService, UserApiService userApiService)
        {
            _userService = userService;
            _userApiService = userApiService;
        }

        // 유저 생성
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
        {
            Console.WriteLine("[web] 유저 생성 요청 발생");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 필수 필드 검증
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Provider))
            {
                throw new ArgumentException("UserId and Provider are required.");
            }

            try
            {
                // UserApiService 호출
                User createdUser = (User)await _userApiService.CreateUserAsync(
                    request.UserId,
                    request.Provider,
                    request.Email,
                    request.Name
                );

                return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.UserId }, createdUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    controller = nameof(UserController),
                    message = ex.Message
                });
            }
        }

        // 유저 정보 읽기
        [HttpGet(("{userId}"))]
        public async Task<IActionResult> GetUserById(string userId)
        {
            Console.WriteLine("[web] 유저 정보 요청 발생");

            try
            {
                // UserApiService를 호출
                var userDetails = await _userApiService.GetUserDetailsForApiAsync(userId);
                return Ok(userDetails);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


    }
}
