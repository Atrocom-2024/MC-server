using Microsoft.Extensions.DependencyInjection;

using MC_server.Core.Extensions;
using MC_server.GameRoom.Handlers;
using MC_server.GameRoom.Managers;
using MC_server.GameRoom.Service;
using MC_server.GameRoom.Communication;

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
            serviceCollection.AddSingleton<GameRoomManager>();
            serviceCollection.AddSingleton<ClientManager>();
            serviceCollection.AddSingleton<ClientMessageSender>();
            serviceCollection.AddSingleton<BroadcastMessageSender>();

            serviceCollection.AddScoped<GameRoomHandler>();
            serviceCollection.AddScoped<UserTcpService>();
            serviceCollection.AddScoped<GameTcpService>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
