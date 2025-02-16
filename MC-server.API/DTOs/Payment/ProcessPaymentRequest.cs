namespace MC_server.API.DTOs.Payment
{
   public class ProcessPaymentRequest
   {
       public string UserId { get; set; } = string.Empty;
       public required string Receipt { get; set; }
       public string Store { get; set; } = string.Empty;
   }
}
