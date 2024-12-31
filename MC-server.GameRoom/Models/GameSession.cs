using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class GameSession
    {
        [ProtoMember(1)]
        public string SessionId { get; set; } = string.Empty;

        [ProtoMember(2)]
        public int? RoomType { get; set; }

        [ProtoMember(3)]
        public long TotalBetAmount { get; set; }

        [ProtoMember(4)]
        public int TotalUser { get; set; }

        [ProtoMember(5)]
        public long TotalJackpotAmount { get; set; }

        [ProtoMember(6)]
        public bool IsJackpot { get; set; }

        [ProtoMember(7)]
        public DateTime CreatedAt { get; set; }
    }
}
