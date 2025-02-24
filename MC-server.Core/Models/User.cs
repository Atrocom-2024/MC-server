using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC_server.Core.Models
{
    [Table("tb_user")]
    public class User
    {
        [Key]
        [Column("user_id")] // Primary Key 명시
        public string UserId { get; set; } = string.Empty;
        
        [Column("nickname"), Required] // Not Null 제약 조건
        public string Nickname { get; set; } = string.Empty;

        [Column("coins")]
        public long Coins { get; set; }

        [Column("level")]
        public int Level { get; set; }

        [Column("experience")]
        public long Experience { get; set; }

        [Column("provider"), Required]
        public string Provider { get; set; } = string.Empty;

        [Column("created_at", TypeName = "datetime2")] // 데이터 타입 명시
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] // 데이터베이스에서 기본값 설정
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Payment>? Payments { get; set; } // User와 Payment 관계 추가
    }
}
