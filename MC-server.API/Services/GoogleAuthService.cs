using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace MC_server.API.Services
{
    public class GoogleAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly GoogleAuthorizationCodeFlow _flow;

        public GoogleAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var clientId = Environment.GetEnvironmentVariable("GOOGLE_AUTH_CLIENT_ID")
                ?? throw new InvalidOperationException("환경변수를 불러오지 못했습니다.");
            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_AUTH_CLIENT_SECRET")
                ?? throw new InvalidOperationException("환경변수를 불러오지 못했습니다.");

            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[]
                {
                    "openid",
                    "https://www.googleapis.com/auth/userinfo.email",
                    "https://www.googleapis.com/auth/userinfo.profile"
                }
            });
        }

        public async Task<GoogleTokenResponse> ExchangeAuthCodeForTokenAsync(string userId, string authCode)
        {
            if (string.IsNullOrWhiteSpace(authCode))
                throw new ArgumentException("Auth code cannot be null or empty.", nameof(authCode));

            var token = await _flow.ExchangeCodeForTokenAsync(
                userId: userId,
                code: authCode,
                redirectUri: null,
                taskCancellationToken: CancellationToken.None
            );

            return new GoogleTokenResponse
            {
                AccessToken = token.AccessToken,
                IdToken = token.IdToken,
                ExpiresIn = token.ExpiresInSeconds ?? 0
            };
            //var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_AUTH_CLIENT_ID") ?? throw new InvalidOperationException("환경변수를 불러오지 못했습니다.");
            //var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_AUTH_CLIENT_SECRET") ?? throw new InvalidOperationException("환경변수를 불러오지 못했습니다.");

            //var requestData = new Dictionary<string, string>
            //{
            //    { "code", authCode },
            //    { "client_id", googleClientId },
            //    { "client_secret", googleClientSecret },
            //    { "redirect_uri", "" }, // 모바일 앱은 redirect_uri 필요 없음
            //    { "grant_type", "authorization_code" },
            //    { "scope", "openid email profile" } // 필요한 권한 추가
            //};

            //var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(requestData));
            //var content = await response.Content.ReadAsStringAsync();

            //if (!response.IsSuccessStatusCode)
            //{
            //    throw new InvalidOperationException($"Failed to exchange auth code for token. Status Code: {response.StatusCode}, Response: {content}");
            //}

            //var tokenResponse = JsonConvert.DeserializeObject<GoogleTokenResponse>(content) ?? throw new InvalidOperationException("Failed to parse Google token response.");
            //return tokenResponse;
        }

        public async Task<GoogleUserInfo> GetUserInfo(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                throw new ArgumentException("Access token is missing or empty.");
            }

            Console.WriteLine($"Id Token: {idToken}");

            // Google의 OAuth2 라이브러리를 사용하여 Payload 검증
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

            // Payload 정보를 GoogleUserInfo 객체로 매핑
            return new GoogleUserInfo
            {
                Id = payload.Subject,
                Email = payload.Email,
                VerifiedEmail = payload.EmailVerified,
                Name = payload.Name,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName,
                Picture = payload.Picture,
            };
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            //var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/userinfo?access_token=${accessToken}");
            //if (!response.IsSuccessStatusCode)
            //{
            //    string errorMessage = $"Google API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            //    throw new HttpRequestException(errorMessage);
            //}

            //var content = await response.Content.ReadAsStringAsync();
            //var userInfo = JsonConvert.DeserializeObject<GoogleUserInfo>(content) ?? throw new InvalidOperationException("Failed to parse Google User Info response.");
            //return userInfo;
        }
    }

    public class GoogleTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }
    }

    public class GoogleUserInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;
        [JsonProperty("verified_email")]
        public bool VerifiedEmail { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("given_name")]
        public string GivenName { get; set; } = string.Empty;
        [JsonProperty("family_name")]
        public string FamilyName { get; set; } = string.Empty;
        [JsonProperty("link")]
        public string Link { get; set; } = string.Empty;
        [JsonProperty("picture")]
        public string Picture { get; set; } = string.Empty;
    }
}
