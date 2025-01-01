using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class BetRequest
    {
        [ProtoMember(1)]
        public string UserId { get; set; } = string.Empty;

        [ProtoMember(2)]
        public int BetAmount { get; set; }

        [ProtoMember(3)]
        public int RoomType { get; set; }
    }
}
