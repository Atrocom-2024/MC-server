using MC_server.API.Services;

namespace MC_server.API.Extensions
{
    public static class ApiServiceExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            // API 서비스 등록
            services.AddScoped<UserApiService>();
            services.AddScoped<GoogleAuthService>();
            services.AddScoped<DailySpinApiService>();

            services.AddHttpClient<PaymentApiService>();

            return services;
        }
    }
}
