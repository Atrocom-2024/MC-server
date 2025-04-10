using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC_server.Core.Models
{
    [Table("tb_dailyspin")]
    public class DailySpin
    {
        [Key]
        [ForeignKey("User")]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("last_spin_time", TypeName = "datetime2")] // 데이터 타입 명시
        public DateTime LastSpinTime { get; set; }

        public virtual User? User { get; set; }
    }
}
