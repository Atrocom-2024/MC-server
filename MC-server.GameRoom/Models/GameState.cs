using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class GameState
    {
        [ProtoMember(1)]
        public long TotalJackpotAmount { get; set; }

        [ProtoMember(2)]
        public bool IsJackpot { get; set; }
    }
}
