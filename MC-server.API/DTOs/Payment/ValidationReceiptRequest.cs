namespace MC_server.API.DTOs.Payment
{
    public class ValidationReceiptRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Receipt { get; set; } = string.Empty;
    }
}
