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

        [HttpPost("verify")]
        public async Task<IActionResult> ProcessPayment([FromBody] ValidationReceiptRequest request)
        {
            Console.WriteLine("[web] 결제 처리 요청");

            if (request == null || request.Receipt == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrWhiteSpace(request.Store))
            {
                Console.WriteLine("[web] Invalid request payload");
                return BadRequest("Invalid request payload.");
            }

            try
            {
                // 1. 영수증 검증
                var validationResult = await _paymentApiService.ValidationReceiptAsync(request.Receipt, request.Store); 

                if (!validationResult.IsValid)
                {
                    return BadRequest(new ProcessPaymentResponse 
                    {
                        IsProcessed = false,
                        TranscationId = validationResult.TransactionId,
                        ProcessedResultCoins = 0,
                        Message = "Invalid receipt.",
                    });
                }

                // 2. 사용자에게 코인 지급 처리
                var processReceiptResult = await _paymentApiService.ProcessReceiptAsync(request.UserId, validationResult.PurchasedCoins);

                if (!processReceiptResult.IsProcessed)
                {
                    return StatusCode(500, new ProcessPaymentResponse
                    {
                        IsProcessed = false,
                        TranscationId = validationResult.TransactionId,
                        ProcessedResultCoins = 0,
                        Message = "An unexpected error occurred. Please try again later"
                    });
                }

                return Ok(new ProcessPaymentResponse
                {
                    IsProcessed = true,
                    TranscationId = validationResult.TransactionId,
                    ProcessedResultCoins = processReceiptResult.ProcessedResultCoins,
                    Message = "Payment successfully.",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while validating the receipt", error = ex.Message });
            }
        }
    }

    public class ProcessPaymentResponse
    {
        public bool IsProcessed { get; set; }
        public string TranscationId { get; set; } = string.Empty;
        public long ProcessedResultCoins { get; set; }
        public string Message { get; set; } = string.Empty;

    }
}
