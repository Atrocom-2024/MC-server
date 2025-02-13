using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC_server.Core.Models
{
    [Table("tb_room")]
    public class Room
    {
        [Key]
        [Column("room_id")]
        public int RoomId { get; set; }

        [Column("target_payout", TypeName = "decimal(10, 7)")]
        public decimal TargetPayout { get; set; }

        [Column("max_bet_amount")]
        public long MaxBetAmount { get; set; }

        [Column("max_user")]
        public int MaxUser { get; set; }

        [Column("base_jackpot_amount")]
        public long BaseJackpotAmount { get; set; }

        [Column("updated_at", TypeName = "datetime2")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] // 데이터베이스에서 기본값 설정
        public DateTime UpdatedAt { get; set; }

        public virtual GameRecord? GameRecord { get; set; }
    }
}
