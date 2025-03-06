using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class GameSessionEndResponse
    {
        [ProtoMember(1)]
        public long RewardCoins { get; set; }
    }
}
