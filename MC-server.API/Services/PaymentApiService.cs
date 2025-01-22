using System.Text.Json;

namespace MC_server.API.Services
{
    public class PaymentApiService
    {
        //public async Task<ValidationReceiptResult> ValidationReceiptAsync(string receipt, string store)
        //{
        //    switch (store.ToLower())
        //    {
        //        case "google":
        //            return await ValidationGooglePlayReceiptAsync(receipt);
        //        default:
        //            throw new ArgumentException("Unsupported store type");
        //    }
        //}

        //public async Task<ValidationReceiptResult> ValidationGooglePlayReceiptAsync(string receipt)
        //{
        //    // 영수증 JSON 파싱
        //    GooglePlayReceipt googleReceipt = JsonSerializer.Deserialize<GooglePlayReceipt>(receipt) ?? throw new JsonException("Failed to deserialize Google Play receipt.");

        //    // API 호출에 필요한 데이터 추출
        //    var purchaseData = googleReceipt.Payload.json;

        //    // Google Play API 호출 URL 생성
        //    string url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/com.Atrocom.MerryCasino/purchases/products/{purchaseData.productId}/tokens/{purchaseData.purchaseToken}";


        //}
    }

    public class ValidationReceiptResult
    {
        public bool IsValid { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public int PurchasedCoins { get; set; }
    }

    public class GooglePlayReceipt
    {
        public string Store { get; set; } = string.Empty;
        public string TransactionID { get; set; } = string.Empty;
        public required GooglePlayPayload Payload { get; set; }
    }

    public class GooglePlayPayload
    {
        public required GooglePlayJson json { get; set; }
        public string signature { get; set; } = string.Empty;
    }

    public class GooglePlayJson
    {
        public string orderId { get; set; } = string.Empty;
        public string packageName { get; set; } = string.Empty;
        public string productId { get; set; } = string.Empty;
        public string purchaseToken { get; set; } = string.Empty;
    }
}
