using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class JoinRoomRequest
    {
        [ProtoMember(1)]
        public string UserId { get; set; } = string.Empty;

        [ProtoMember(2)]
        public int RoomId { get; set; }
    }
}
