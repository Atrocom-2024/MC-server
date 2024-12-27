using System;
using MC_server.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MC_server.Core.Extensions
{
    public static class CoreServiceExtensions // 확장 메서드는 static 클래스 안에서 정의돼야 함
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddScoped<UserService>();
            services.AddScoped<RoomService>();

            return services;
        }
    }
}
