using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class ClientResponse
    {
        [ProtoMember(1)]
        public string ResponseType { get; set; } = string.Empty; // BetResponse, GameState, GameUserState, AddCoinsResponse

        [ProtoMember(2)]
        public GameUserState? GameUserState { get; set; }

        [ProtoMember(3)]
        public GameState? GameState { get; set; }

        [ProtoMember(4)]
        public BetResponse? BetResponseData { get; set; }

        [ProtoMember(5)]
        public AddCoinsResponse? AddCoinsResponseData { get; set; }
    }
}
