using DotNetEnv;
using MC_server.Core;
using MC_server.Core.Extensions;
using MC_server.API.Extensions;

namespace MC_server.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<ApplicationDbContext>(ApplicationDbContext.Configure);

            // Core 및 API 서비스 등록
            // .NET Core의 의존성 주입(Dependency Injection, DI)
            // 모든 서비스는 DI 컨테이너에 등록되어야 런타임에서 사용 가능
            builder.Services.AddCoreServices();
            builder.Services.AddApiServices();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
