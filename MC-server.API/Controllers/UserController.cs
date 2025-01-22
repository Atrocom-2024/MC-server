using Microsoft.AspNetCore.Mvc;

using MC_server.API.DTOs.User;
using MC_server.API.Services;
using MC_server.Core.Models;

namespace MC_server.API.Controllers
{
    [ApiController]
    [Route("/api/users")]
    public class UserController : ControllerBase
    {
        private readonly UserApiService _userApiService;

        public UserController(UserApiService userApiService)
        {
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
                User createdUser = (User)await _userApiService.CreateUserAsync(request);

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
        public async Task<IActionResult> GetUserById([FromRoute] string userId)
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

        // 유저 정보 수정
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser([FromRoute] string userId, [FromBody] UserUpdateRequest request)
        {
            // 허용된 필드 확인
            var allowedKeys = new[] { "nickname", "addCoins", "level", "experience" };
            var invalidKeys = request.GetInvalidKeys(allowedKeys);

            if (invalidKeys.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Invalid fields in request body",
                    invalidFields = invalidKeys
                });
            }

            try
            {
                // 유저 업데이트 요청 처리
                var updatedFields = await _userApiService.UpdateUserAsync(userId, request);
                if (updatedFields == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new
                {
                    message = "User updated successfully",
                    updatedFields
                });
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
    }
}
