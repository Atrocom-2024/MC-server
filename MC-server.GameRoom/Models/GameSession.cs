using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class GameSession
    {
        [ProtoMember(1)]
        public long TotalBetAmount { get; set; }

        [ProtoMember(2)]
        public int TotalUser { get; set; }

        [ProtoMember(3)]
        public long TotalJackpotAmount { get; set; }

        [ProtoMember(4)]
        public bool IsJackpot { get; set; }

        [ProtoMember(5)]
        public decimal TargetPayout { get; set; }

        [ProtoMember(6)]
        public long MaxBetAmount { get; set; }

        [ProtoMember(7)]
        public int MaxUser { get; set; }

        [ProtoMember(8)]
        public long BaseJackpotAmount { get; set; }

        [ProtoMember(9)]
        public DateTime CreatedAt { get; set; }
    }
}
