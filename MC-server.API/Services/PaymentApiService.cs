using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth.OAuth2;
using MC_server.Core.Models;
using MC_server.Core.Services;

namespace MC_server.API.Services
{
    public class PaymentApiService
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;
        private readonly PaymentService _paymentService;
        
        public PaymentApiService(HttpClient httpClient, UserService userService, PaymentService paymentService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        }

        // JSON 영수증 파싱 메서드
        public GooglePlayReceiptJson DeserializeReceiptAsync(string receiptJson)
        {
            // 1️⃣ 최상위 JSON 파싱
            var googleReceiptRoot = JsonSerializer.Deserialize<GooglePlayReceipt>(receiptJson)
                ?? throw new JsonException("Failed to deserialize GooglePlayReceipt.");

            // 2️⃣ Payload JSON 변환
            var googleReceiptPayload = JsonSerializer.Deserialize<GooglePlayReceiptPayload>(googleReceiptRoot.Payload)
                ?? throw new JsonException("Failed to deserialize GooglePlayReceiptPayload.");

            // 3️⃣ Payload.json 필드도 다시 JSON 변환
            var googleReceipt = JsonSerializer.Deserialize<GooglePlayReceiptJson>(googleReceiptPayload.Json)
                ?? throw new JsonException("Failed to deserialize GooglePlayReceiptJson.");

            return googleReceipt;
        }

        // 영수증 검증 메서드 -> 서비스에 따라 switch 문으로 분류
        public async Task<ValidationReceiptResult> ValidationReceiptAsync(GooglePlayReceiptJson receipt, string store)
        {
            switch (store.ToLower())
            {
                case "google":
                    return await ValidationGooglePlayReceiptAsync(receipt);
                default:
                    throw new NotSupportedException($"Unsupported store type: {store}");
            }
        }

        public async Task<ValidationReceiptResult> ValidationGooglePlayReceiptAsync(GooglePlayReceiptJson receipt)
        {
            // 1. 액세스 토큰 가져오기
            string accessToken = await GetAccessTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Failed to retrieve access token.");
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("[web] Access Token을 가져오는 데 실패했습니다.");
                return new ValidationReceiptResult
                {
                    IsValid = false,
                    TransactionId = receipt.OrderId,
                    PurchasedCoins = 0
                };
            }

            // 2. Google Play API 호출 URL 생성
            string url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{receipt.PackageName}/purchases/products/{receipt.ProductId}/tokens/{receipt.PurchaseToken}";

            // 3. HTTP 요청 생성(Authorization 헤더 포함)
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // 4. 구글 서버로 요청
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            // 요청 실패 처리
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Receipt validation request failed. StatusCode: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
            }

            // 5. 응답 JSON 파싱
            var validationResponse = JsonSerializer.Deserialize<GooglePlayValidationResponse>(responseContent)
                ?? throw new JsonException(responseContent);

            // 구매 상태 체크
            if (validationResponse.purchaseState != 0) // 구매되지 않았을 때
            {
                throw new InvalidOperationException("구매되지 않은 상품입니다.");
            }

            return new ValidationReceiptResult
            {
                IsValid = true,
                TransactionId = receipt.OrderId,
                PurchasedCoins = CalculatePurchasedCoins(receipt.ProductId)
            };
        }

        public async Task CreatePaymentRecord(string userId, GooglePlayReceiptJson receiptPayload, string rawReceipt)
        {
            var payment = new Payment
            {
                UserId = userId,
                OrderId = receiptPayload.OrderId,
                ProductId = receiptPayload.ProductId,
                PurchaseToken = receiptPayload.PurchaseToken,
                PurchaseState = receiptPayload.PurchaseState,
                PurchaseTime = DateTimeOffset.FromUnixTimeMilliseconds(receiptPayload.PurchaseTime).UtcDateTime,
                ReceiptData = rawReceipt
            };
            await _paymentService.CreatePaymentAsync(payment);
        }

        public async Task<ProcessReceiptResult> ProcessReceiptAsync(string userId, int addCoinsAmount)
        {
            var user = await _userService.GetUserByIdAsync(userId) ?? throw new KeyNotFoundException($"User with ID '{userId}' not found.");

            user.Coins += addCoinsAmount;
            await _userService.UpdateUserAsync(user);
            return new ProcessReceiptResult { IsProcessed = true, ProcessedResultCoins = user.Coins };
        }

        private static async Task<string> GetAccessTokenAsync()
        {
            // 1. 환경변수에서 JSON 키 파일 내용 가져오기
            string? jsonKeyFilePath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            if (string.IsNullOrEmpty(jsonKeyFilePath))
            {
                throw new InvalidOperationException("환경변수를 불러오지 못했습니다.");
            }

            // 2. JSON 키 파일 존재 여부 확인
            if (!File.Exists(jsonKeyFilePath))
            {
                throw new FileNotFoundException($"Google 서비스 계정 JSON 키 파일을 찾을 수 없습니다: {jsonKeyFilePath}");
            }

            // 3. JSON 키를 메모리 스트림으로 변환하여 GoogleCredentials 로드
            using var stream = new FileStream(jsonKeyFilePath, FileMode.Open, FileAccess.Read);

            var credentials = GoogleCredential.FromStream(stream)
                .CreateScoped(new[] { "https://www.googleapis.com/auth/androidpublisher" });

            var serviceAccountEmail = ((ServiceAccountCredential)credentials.UnderlyingCredential).Id;
            Console.WriteLine($"현재 인증된 서비스 계정 이메일: {serviceAccountEmail}");

            // 액세스 토큰 요청
            return await credentials.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }

        private static int CalculatePurchasedCoins(string productId)
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

    public class GooglePlayReceiptJson
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
