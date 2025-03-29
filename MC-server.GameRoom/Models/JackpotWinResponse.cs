using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class JackpotWinResponse
    {
        [ProtoMember(1)]
        public long AddedCoinsAmount { get; set; }
    }
}
