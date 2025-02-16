using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth.OAuth2;

using MC_server.API.DTOs.Payment;
using MC_server.Core.Services;

namespace MC_server.API.Services
{
    public class PaymentApiService
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;
        
        public PaymentApiService(HttpClient httpClient, UserService userService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        // JSON 영수증 파싱 메서드
        public GooglePlayReceiptJson2? DeserializeReceiptAsync(string receiptJson)
        {
            try
            {
                // 1️⃣ 최상위 JSON 파싱
                var googleReceiptRoot = JsonSerializer.Deserialize<GooglePlayReceipt2>(receiptJson)
                    ?? throw new JsonException("Failed to deserialize GooglePlayReceipt.");

                // 2️⃣ Payload JSON 변환
                var googleReceiptPayload = JsonSerializer.Deserialize<GooglePlayReceiptPayload>(googleReceiptRoot.Payload)
                    ?? throw new JsonException("Failed to deserialize GooglePlayReceiptPayload.");

                // 3️⃣ Payload.json 필드도 다시 JSON 변환
                var googleReceipt = JsonSerializer.Deserialize<GooglePlayReceiptJson2>(googleReceiptPayload.Json)
                    ?? throw new JsonException("Failed to deserialize GooglePlayReceiptJson.");

                return googleReceipt;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeserializeReceipt: {ex.Message}");
                return null;
            }
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
            // 1. 액세스 토큰 가져오기
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

            // 2. Google Play API 호출 URL 생성
            string url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{receipt.packageName}/purchases/products/{receipt.productId}/tokens/{receipt.purchaseToken}";

            // 3. HTTP 요청 생성(Authorization 헤더 포함)
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // 4. 구글 서버로 요청
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            // 요청 실패 처리
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("[web] 영수증 검증 요청에 실패했습니다.");
                return new ValidationReceiptResult
                {
                    IsValid = false,
                    TransactionId = receipt.orderId,
                    PurchasedCoins = 0
                };
            }

            // 5. 응답 JSON 파싱
            var validationResponse = JsonSerializer.Deserialize<GooglePlayValidationResponse>(responseContent)
                ?? throw new JsonException(responseContent);

            // 구매 상태 체크
            if (validationResponse.purchaseState != 0) // 구매되지 않았을 때
            {
                Console.WriteLine("[web] 구매되지 않은 상품입니다.");
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

        public async Task<ProcessReceiptResult> ProcessReceiptAsync(string userId, int addCoinsAmount)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    return new ProcessReceiptResult { IsProcessed = false, ProcessedResultCoins = 0 };
                }

                user.Coins += addCoinsAmount;
                await _userService.UpdateUserAsync(user);
                return new ProcessReceiptResult { IsProcessed = true, ProcessedResultCoins = user.Coins };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessPayment: {ex.Message}");
                return new ProcessReceiptResult { IsProcessed= false, ProcessedResultCoins = 0 };
            }
        }

        private static async Task<string> GetAccessTokenAsync()
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

                var serviceAccountEmail = ((ServiceAccountCredential)credentials.UnderlyingCredential).Id;
                Console.WriteLine($"현재 인증된 서비스 계정 이메일: {serviceAccountEmail}");

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

    public class ProcessReceiptResult
    {
        public bool IsProcessed { get; set; }
        public long ProcessedResultCoins { get; set; }
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
        public string purchaseTime { get; set; } = string.Empty;
        public string purchaseState { get; set; } = string.Empty;
        public string productId { get; set; } = string.Empty;
        public string purchaseToken { get; set; } = string.Empty;
    }

    public class GooglePlayReceipt2
    {
        [JsonPropertyName("Payload")]
        public string Payload { get; set; } = string.Empty;

        [JsonPropertyName("Store")]
        public string Store { get; set; } = string.Empty;

        [JsonPropertyName("TransactionID")]
        public string TransactionID { get; set; } = string.Empty;
    }

    public class GooglePlayReceiptPayload
    {
        [JsonPropertyName("json")]
        public string Json { get; set; } = string.Empty;

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;
    }

    public class GooglePlayReceiptJson2
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("packageName")]
        public string PackageName { get; set; } = string.Empty;

        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("purchaseTime")]
        public long PurchaseTime { get; set; }

        [JsonPropertyName("purchaseState")]
        public int PurchaseState { get; set; }

        [JsonPropertyName("purchaseToken")]
        public string PurchaseToken { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("acknowledged")]
        public bool Acknowledged { get; set; }
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
