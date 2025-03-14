using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class ClientResponse
    {
        [ProtoMember(1)]
        public string ResponseType { get; set; } = string.Empty; // BetResponse, GameState, GameUserState, AddCoinsResponse, GameSessionEnd, JackpotWinResponse

        [ProtoMember(2)]
        public string? ErrorMessage { get; set; } = string.Empty;

        [ProtoMember(3)]
        public GameUserState? GameUserState { get; set; }

        [ProtoMember(4)]
        public GameState? GameState { get; set; }

        [ProtoMember(5)]
        public BetResponse? BetResponseData { get; set; }

        [ProtoMember(6)]
        public AddCoinsResponse? AddCoinsResponseData { get; set; }

        [ProtoMember(7)]
        public GameSessionEnd? GameSessionEndData { get; set; }

        [ProtoMember(8)]
        public JackpotWinResponse? JackpotWinResponseData { get; set; }
    }
}
