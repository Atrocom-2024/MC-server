using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class RoomJoinRequest
    {
        [ProtoMember(1)]
        public int RoomId { get; set; }
    }
}
