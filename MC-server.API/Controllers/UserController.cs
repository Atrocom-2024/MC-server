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

            // UserApiService 호출
            User createdUser = await _userApiService.CreateUserAsync(request.UserId, request.Provider);

            return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.UserId }, createdUser);
        }

        // 유저 정보 읽기
        [HttpGet(("{userId}"))]
        public async Task<IActionResult> GetUserById([FromRoute] string userId)
        {
            Console.WriteLine("[web] 유저 정보 요청 발생");

            // UserApiService를 호출
            var userDetails = await _userApiService.GetUserDetailsForApiAsync(userId)
                ?? throw new KeyNotFoundException($"User with ID '{userId}' not found.");

            return Ok(userDetails);
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
    }
}
