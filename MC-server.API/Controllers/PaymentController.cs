using Microsoft.AspNetCore.Mvc;

using MC_server.API.DTOs.Payment;
using MC_server.API.Services;
using System.ComponentModel.DataAnnotations;

namespace MC_server.API.Controllers
{
    [ApiController]
    [Route("/api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentApiService _paymentApiService;

        public PaymentController(PaymentApiService paymentApiService)
        {
            _paymentApiService = paymentApiService;
        }

        [HttpPost("verify")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            Console.WriteLine("[web] 결제 처리 요청");

            if (request == null || request.Receipt == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrWhiteSpace(request.Store))
            {
                throw new ValidationException("Receipt and UserId and Store are required.");
            }

            // 1. 영수증 파싱
            GooglePlayReceiptJson deserializedReceipt = _paymentApiService.DeserializeReceiptAsync(request.Receipt);
            
            // 2. 영수증 검증
            var validationResult = await _paymentApiService.ValidationReceiptAsync(deserializedReceipt, request.Store);

            if (!validationResult.IsValid)
            {
                return BadRequest(new ProcessPaymentResponse
                {
                    IsProcessed = validationResult.IsValid,
                    TranscationId = validationResult.TransactionId,
                    ProcessedResultCoins = 0,
                    Message = "Invalid receipt.",
                });
            }

            // 3. 결제 내역 저장
            await _paymentApiService.CreatePaymentRecord(request.UserId, deserializedReceipt, request.Receipt);

            // 3. 사용자에게 코인 지급 처리
            var processReceiptResult = await _paymentApiService.ProcessReceiptAsync(request.UserId, validationResult.PurchasedCoins);

            if (!processReceiptResult.IsProcessed)
            {
                return StatusCode(500, new ProcessPaymentResponse
                {
                    IsProcessed = processReceiptResult.IsProcessed,
                    TranscationId = validationResult.TransactionId,
                    ProcessedResultCoins = 0,
                    Message = "An unexpected error occurred. Please try again later"
                });
            }

            return Ok(new ProcessPaymentResponse
            {
                IsProcessed = processReceiptResult.IsProcessed,
                TranscationId = validationResult.TransactionId,
                ProcessedResultCoins = processReceiptResult.ProcessedResultCoins,
                Message = "Payment successfully.",
            });
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
