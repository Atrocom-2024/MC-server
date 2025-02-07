using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC_server.Core.Models
{
    [Table("tb_game_record")]
    public class GameRecord
    {
        [Key]
        [Column("game_id")]
        public string GameId { get; set; } = string.Empty;

        [ForeignKey("Room")]
        [Column("room_type")]
        public int RoomType { get; set; }

        [Column("total_bet_amount")]
        public long TotalBetAmount { get; set; }

        [Column("total_user")]
        public int TotalUser { get; set; }

        [Column("total_jackpot_amount")]
        public long TotalJackpotAmount { get; set; }

        [Column("is_jackpot")]
        public bool IsJackpot { get; set; }

        [Column("created_at", TypeName = "datetime2")] // 데이터 타입 명시
        public DateTime CreatedAt { get; set; }

        public Room? Room { get; set; }
    }
}
