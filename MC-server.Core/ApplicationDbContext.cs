using MC_server.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MC_server.Core
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public static void Configure(DbContextOptionsBuilder options)
        {
            string? db = Environment.GetEnvironmentVariable("DB_DATABASE");
            string? host = Environment.GetEnvironmentVariable("DB_HOST");
            string? port = Environment.GetEnvironmentVariable("DB_PORT");
            string? username = Environment.GetEnvironmentVariable("DB_USERNAME");
            string? password = Environment.GetEnvironmentVariable("DB_PASSWORD");
            string connectionString = $"Server={host};Port={port};Database={db};User={username};Password={password};";

            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 39)));
        }

        // 엔티티 정의
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<GameRecord> Games { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<DailySpin> DailySpins { get; set; }
    }
}
