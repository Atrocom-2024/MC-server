using DotNetEnv;
using MC_server.API.Extensions;
using MC_server.API.Middleware;
using MC_server.Core.Extensions;

namespace MC_server.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // .env 파일 로드
            Env.Load();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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

            // 예외 처리 미들웨어 추가 (가장 먼저 실행)
            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
