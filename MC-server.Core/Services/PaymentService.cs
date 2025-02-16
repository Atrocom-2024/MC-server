using MC_server.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MC_server.Core.Services
{
    public class PaymentService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PaymentService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 데이터 검증
            if (await dbContext.Payments.AnyAsync(p => p.PaymentId == payment.PaymentId))
            {
                throw new InvalidOperationException($"Payment with ID '{payment.PaymentId}' already exists");
            }

            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();
            return payment;
        }
    }
}
