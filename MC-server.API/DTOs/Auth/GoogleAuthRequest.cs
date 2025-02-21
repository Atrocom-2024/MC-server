namespace MC_server.API.DTOs.Auth
{
    public class GoogleAuthRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string AuthCode { get; set; } = string.Empty;
    }
}
