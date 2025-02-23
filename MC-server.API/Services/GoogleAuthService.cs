using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;
using Newtonsoft.Json;

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
        }

        public async Task<Tokeninfo> ValidationAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Access token is missing or empty.");
            }

            Console.WriteLine($"Access Token: {accessToken}");

            var oauthService = new Oauth2Service(new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
                ApplicationName = "MerryCasino"
            });

            var tokenInfo = await oauthService.Tokeninfo().ExecuteAsync();
            return tokenInfo;
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
