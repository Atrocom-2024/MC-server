using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class BetResponse
    {
        [ProtoMember(1)]
        public long UpdatedCoins { get; set; }
    }
}
