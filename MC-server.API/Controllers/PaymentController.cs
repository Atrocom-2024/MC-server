using Microsoft.AspNetCore.Mvc;

using MC_server.API.DTOs.Payment;
using MC_server.API.Services;

namespace MC_server.API.Controllers
{
    [ApiController]
    [Route("/api/payments")]
    public class PaymentController
    {
        private readonly UserApiService _userApiService;
        private readonly PaymentApiService _paymentApiService;

        public PaymentController(UserApiService userApiService, PaymentApiService paymentApiService)
        {
            _userApiService = userApiService;
            _paymentApiService = paymentApiService;
        }

        [HttpPost("validate-receipt")]
        public async Task ValidationReceipt([FromBody] ValidationReceiptRequest request)
        {
            // 영수증 검증
            //var validationReceipt = await 
        }
    }
}
