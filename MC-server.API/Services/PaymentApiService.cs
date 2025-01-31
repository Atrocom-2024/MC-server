using Google.Apis.Auth.OAuth2;
using MC_server.API.DTOs.Payment;
using System.Text.Json;

namespace MC_server.API.Services
{
    public class PaymentApiService
    {
        private readonly HttpClient _httpClient;
        
        public PaymentApiService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // 영수증 검증 메서드 -> 서비스에 따라 switch 문으로 분류
        public async Task<ValidationReceiptResult> ValidationReceiptAsync(GooglePlayReceiptJson receipt, string store)
        {
            switch (store.ToLower())
            {
                case "google":
                    return await ValidationGooglePlayReceiptAsync(receipt);
                default:
                    throw new ArgumentException("Unsupported store type");
            }
        }

        // 구글 플레이 영수증 검증 메서드
        public async Task<ValidationReceiptResult> ValidationGooglePlayReceiptAsync(GooglePlayReceiptJson receipt)
        {
            // ------------------------------ 액세스 토큰 받아오는 부분 ------------------------------

            string accessToken = await GetAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("[web] Access Token을 가져오는 데 실패했습니다.");
                return new ValidationReceiptResult
                {
                    IsValid = false,
                    TransactionId = receipt.orderId,
                    PurchasedCoins = 0
                };
            }

            Console.WriteLine($"Access Token: {accessToken}");
            // ------------------------------------------------------------------------------------

            //// 영수증 JSON 파싱
            //var googleReceipt = JsonSerializer.Deserialize<GooglePlayReceipt>(receipt) ?? throw new JsonException("Failed to deserialize Google Play receipt.");

            //// API 호출에 필요한 데이터 추출
            //var purchaseData = googleReceipt.Payload.json;

            // Google Play API 호출 URL 생성
            string url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{receipt.packageName}/purchases/products/{receipt.productId}/tokens/{receipt.purchaseToken}";

            // 구글 서버로 요청
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            Console.WriteLine(response.StatusCode);
            // 요청 실패 처리
            if (!response.IsSuccessStatusCode)
            {
                return new ValidationReceiptResult
                {
                    IsValid = false,
                    TransactionId = receipt.orderId,
                    PurchasedCoins = 0
                };
            }

            // 요청 성공 처리
            string responseContent = await response.Content.ReadAsStringAsync();
            var validationResponse = JsonSerializer.Deserialize<GooglePlayValidationResponse>(responseContent)
                ?? throw new JsonException(responseContent);

            // 구매 상태 체크
            if (validationResponse.purchaseState != 0) // 구매되지 않았을 때
            {
                return new ValidationReceiptResult
                {
                    IsValid = false,
                    TransactionId = receipt.orderId,
                    PurchasedCoins = 0
                };
            }

            return new ValidationReceiptResult
            {
                IsValid = true,
                TransactionId = receipt.orderId,
                PurchasedCoins = CalculatePurchasedCoins(receipt.productId)
            };
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                // 환경변수에서 JSON 키 파일 내용 가져오기
                string? jsonKeyFilePath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

                if (string.IsNullOrEmpty(jsonKeyFilePath))
                {
                    throw new Exception("환경변수를 불러오지 못했습니다.");
                }

                // JSON 키를 메모리 스트림으로 변환하여 GoogleCredentials 로드
                using var stream = new FileStream(jsonKeyFilePath, FileMode.Open, FileAccess.Read);

                var credentials = GoogleCredential.FromStream(stream)
                    .CreateScoped(new[] { "https://www.googleapis.com/auth/androidpublisher" });

                // 액세스 토큰 요청
                return await credentials.UnderlyingCredential.GetAccessTokenForRequestAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"토큰 요청 중 오류 발생: {ex.Message}");
                Console.WriteLine($"상세 오류: {ex.StackTrace}");
                return string.Empty;
            }
        }

        private int CalculatePurchasedCoins(string productId)
        {
            return productId switch
            {
                "coin_pack_1" => 500000,
                "coin_pack_2" => 1000000,
                "coin_pack_3" => 5000000,
                "coin_pack_4" => 10000000,
                _ => 0
            };
        }
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

    public class GooglePlayValidationResponse
    {
        // 이 종류는 androidpublisher 서비스의 inappPurchase 객체
        public string kind { get; set; } = string.Empty;

        // 제품이 구매된 시간을 에포크 기준 시간 (1970년 1월 1일) 이후 밀리초 단위로 나타낸 것
        public string purchaseTimeMillis { get; set; } = string.Empty;

        // 0(구매함), 1(취소됨), 2(대기중)
        public int purchaseState { get; set; }

        // 인앱 상품의 소비 상태 -> 가능한 값은 0(아직 소비되지 않음), 1(소비함)
        public int consumptionState { get; set; }

        // 주문의 추가 정보가 포함된 개발자 지정 문자열
        public string developerPayload { get; set; } = string.Empty;

        // 인앱 상품 구매와 연결된 주문 ID
        public string orderId { get; set; } = string.Empty;

        // 인앱 상품 구매 유형 -> 가능한 값은 0
        public int purchaseType { get; set; }

        // 인앱 상품의 확인 상태 -> 가능한 값은 0
        public int acknowledgementState { get; set; }
        public string purchaseToken { get; set; } = string.Empty;
        public string productId { get; set; } = string.Empty;
        public int quantity { get; set; }
        public string obfuscatedExternalAccountId { get; set; } = string.Empty;
        public string obfuscatedExternalProfileId { get; set; } = string.Empty;
        public string regionCode { get; set; } = string.Empty;
        public int refundableQuantity { get; set; }
    }
}
