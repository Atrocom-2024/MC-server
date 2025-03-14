using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class ClientRequest
    {
        [ProtoMember(1)]
        public string RequestType { get; set; } = string.Empty; // BetRequest, JoinRoomRequest, AddCoinsRequest, JackpotWinRequest

        [ProtoMember(2)]
        public JoinRoomRequest? JoinRoomData { get; set; }

        [ProtoMember(3)]
        public BetRequest? BetData { get; set; }

        [ProtoMember(4)]
        public AddCoinsRequest? AddCoinsData { get; set; }

        [ProtoMember(5)]
        public JackpotWinRequest? JackpotWinData { get; set; }
    }
}
