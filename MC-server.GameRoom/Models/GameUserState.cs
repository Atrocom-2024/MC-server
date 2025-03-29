using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class GameUserState
    {
        [ProtoMember(1)]
        public decimal CurrentPayout { get; set; }

        [ProtoMember(2)]
        public decimal JackpotProb { get; set; }
    }
}
