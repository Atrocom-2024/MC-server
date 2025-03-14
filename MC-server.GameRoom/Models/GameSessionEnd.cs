using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class GameSessionEnd
    {
        [ProtoMember(1)]
        public long RewardedCoinsAmount { get; set; }

        [ProtoMember(2)]
        public int RewardCoins { get; set; }
    }
}
