namespace MC_server.API.DTOs.Auth
{
    public class LoginRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AuthCode { get; set; } = string.Empty;
    }
}
