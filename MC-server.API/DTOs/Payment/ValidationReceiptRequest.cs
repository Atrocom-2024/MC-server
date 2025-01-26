namespace MC_server.API.DTOs.Payment
{
    public class ValidationReceiptRequest
    {
        public string UserId { get; set; } = string.Empty;
        public required GooglePlayReceiptJson Receipt { get; set; }
        public string Store { get; set; } = string.Empty;
    }

    public class GooglePlayReceiptJson
    {
        public string orderId { get; set; } = string.Empty;
        public string packageName { get; set; } = string.Empty;
        public string productId { get; set; } = string.Empty;
        public string purchaseToken { get; set; } = string.Empty;
    }
}
