using Microsoft.Extensions.DependencyInjection;

using MC_server.GameRoom.Handlers;
using MC_server.GameRoom.Managers;
using MC_server.GameRoom.Services;
using MC_server.Core.Extensions;

namespace MC_server.GameRoom.Extensions
{
    public static class ServiceConfigurator
    {
        public static IServiceProvider ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCoreServices();

            // DI 컨테이너에 서비스 등록
            serviceCollection.AddSingleton<Program>();
            serviceCollection.AddSingleton<GameRoomService>();
            serviceCollection.AddSingleton<SessionService>();
            serviceCollection.AddSingleton<ClientManager>();

            serviceCollection.AddScoped<ClientHandler>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
