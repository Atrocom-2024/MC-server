using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC_server.Core.Models
{
    [Table("tb_game")]
    public class Game
    {
        [Key]
        [Column("game_id")]
        public string GameId { get; set; } = string.Empty;

        [Column("room_type")]
        public int RoomType { get; set; }

        [Column("total_bet_amount")]
        public long TotalBet { get; set; }

        [Column("total_user")]
        public int TotalUser { get; set; }

        [Column("total_jackpot_amount")]
        public long TotalJackpotAmount { get; set; }

        [Column("is_jackpot")]
        public bool IsJackpot { get; set; }

        [Column("created_at", TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at", TypeName = "datetime2")]
        public DateTime UpdatedAt { get; set; }

        public Room Room { get; set; } = new Room();
    }
}
