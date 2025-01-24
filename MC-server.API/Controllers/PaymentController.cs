using Microsoft.AspNetCore.Mvc;

using MC_server.API.DTOs.Payment;
using MC_server.API.Services;

namespace MC_server.API.Controllers
{
    [ApiController]
    [Route("/api/payments")]
    public class PaymentController: ControllerBase
    {
        private readonly UserApiService _userApiService;
        private readonly PaymentApiService _paymentApiService;

        public PaymentController(UserApiService userApiService, PaymentApiService paymentApiService)
        {
            _userApiService = userApiService;
            _paymentApiService = paymentApiService;
        }

        [HttpPost("validate-receipt")]
        public async Task<IActionResult> ValidationReceipt([FromBody] ValidationReceiptRequest request)
        {
            Console.WriteLine("[web] 결제 영수증 검증 요청");

            if (request == null || string.IsNullOrWhiteSpace(request.Receipt) || string.IsNullOrWhiteSpace(request.Store))
            {
                return BadRequest("Invalid request payload.");
            }

            // 영수증 검증
            try
            {
                var validationResult = await _paymentApiService.ValidationReceiptAsync(request.Receipt, request.Store); 

                if (!validationResult.IsValid)
                {
                    return BadRequest(new { message = "Invalid receipt.", validationResult.TransactionId });
                }

                // 사용자에게 코인 지급 처리
                return Ok(new
                {
                    message = "Receipt validated successfully.",
                    validationResult.TransactionId,
                    validationResult.PurchasedCoins
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while validating the receipt", error = ex.Message });
            }
        }
    }
}
