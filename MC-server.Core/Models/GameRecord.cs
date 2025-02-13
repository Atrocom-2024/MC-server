using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC_server.Core.Models
{
    [Table("tb_game_record")]
    public class GameRecord
    {
        [Key]
        [ForeignKey("Room")]
        [Column("room_id")]
        public int RoomId { get; set; }

        [Column("total_bet_amount")]
        public long TotalBetAmount { get; set; }

        [Column("total_user")]
        public int TotalUser { get; set; }

        [Column("total_jackpot_amount")]
        public long TotalJackpotAmount { get; set; }

        [Column("is_jackpot")]
        public bool IsJackpot { get; set; }

        [Column("updated_at", TypeName = "datetime2")] // 데이터 타입 명시
        public DateTime UpdatedAt { get; set; }

        public virtual Room? Room { get; set; }
    }
}
