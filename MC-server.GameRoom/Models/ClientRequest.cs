using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class ClientRequest
    {
        [ProtoMember(1)]
        public string RequestType { get; set; } = string.Empty; // "Bet", "JoinRoom"

        [ProtoMember(2)]
        public BetRequest? BetData { get; set; }

        [ProtoMember(3)]
        public RoomJoinRequest? JoinRoomData { get; set; }
    }
}
