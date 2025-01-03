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

        [Column("target_payout", TypeName = "decimal(6, 3)")]
        public decimal TargetPayout { get; set; }

        [Column("max_bet_amount")]
        public long MaxBet { get; set; }

        [Column("max_user")]
        public int MaxUser { get; set; }

        [Column("jackpot_amount")]
        public long JackpotAmount { get; set; }

        [Column("updated_at", TypeName = "datetime2")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] // 데이터베이스에서 기본값 설정
        public DateTime UpdatedAt { get; set; }

        public ICollection<Game> Games { get; set; } = new List<Game>();
    }
}
