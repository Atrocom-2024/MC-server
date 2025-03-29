using System.ComponentModel.DataAnnotations;

namespace MC_server.API.DTOs.User
{
    public class UserCreateRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Provider { get; set; } = string.Empty;
    }
}
