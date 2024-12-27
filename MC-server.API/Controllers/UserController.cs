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
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            User createdUser = await _userService.CreateUserAsync(user);
            return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.UserId }, createdUser);
        }

        // 유저 정보 읽기
        [HttpGet(("{userId}"))]
        public async Task<IActionResult> GetUserById(string userId)
        {
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
