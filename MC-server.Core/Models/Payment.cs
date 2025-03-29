using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC_server.Core.Models
{
    [Table("tb_payment")]
    [Index(nameof(OrderId), IsUnique = true)] // order_id에 UNIQUE 적용
    [Index(nameof(PurchaseToken), IsUnique = true)] // purchase_token에 UNIQUE 적용
    public class Payment
    {
        [Key]
        [Column("payment_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto Increment 설정
        public int PaymentId { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [Column("product_id")]
        public string ProductId { get; set; } = string.Empty;

        [Column("purchase_token")]
        public string PurchaseToken { get; set; } = string.Empty;

        [Column("purchase_state")]
        public int PurchaseState { get; set; }

        [Column("purchase_time", TypeName = "datetime2")]
        public DateTime PurchaseTime { get; set; }

        [Column("receipt_data")]
        public string ReceiptData { get; set; } = string.Empty;

        [Column("created_at", TypeName = "datetime2")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] // 데이터베이스에서 기본값 설정
        public DateTime CreatedAt { get; set; }

        public virtual User? User { get; set; }
    }
}
