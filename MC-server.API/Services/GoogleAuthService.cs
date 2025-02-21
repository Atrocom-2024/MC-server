using Newtonsoft.Json;

namespace MC_server.API.Services
{
    public class GoogleAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GoogleAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<GoogleTokenResponse> ExchangeAuthCodeForTokenAsync(string authCode)
        {
            if (string.IsNullOrWhiteSpace(authCode))
                throw new ArgumentException("Auth code cannot be null or empty.", nameof(authCode));

            var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_AUTH_CLIENT_ID") ?? throw new InvalidOperationException("환경변수를 불러오지 못했습니다.");
            var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_AUTH_CLIENT_SECRET") ?? throw new InvalidOperationException("환경변수를 불러오지 못했습니다.");

            var requestData = new Dictionary<string, string>
            {
                { "code", authCode },
                { "client_id", googleClientId },
                { "client_secret", googleClientSecret },
                { "redirect_uri", "" }, // 모바일 앱은 redirect_uri 필요 없음
                { "grant_type", "authorization_code" }
            };

            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(requestData));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to exchange auth code for token. Status Code: {response.StatusCode}, Response: {content}");
            }

            var tokenResponse = JsonConvert.DeserializeObject<GoogleTokenResponse>(content) ?? throw new InvalidOperationException("Failed to parse Google token response.");
            return tokenResponse;
        }
    }

    public class GoogleTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
