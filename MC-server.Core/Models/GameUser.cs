using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MC_server.Core.Models
{
    [Table("tb_game_user")]
    public class GameUser
    {
        [Key]
        [Column("game_user_id")]
        public string GameUserId { get; set; } = string.Empty;

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

    }
}
