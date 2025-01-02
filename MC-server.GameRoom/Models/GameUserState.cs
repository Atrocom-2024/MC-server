using ProtoBuf;

namespace MC_server.GameRoom.Models
{
    [ProtoContract]
    public class GameUserState
    {
        [ProtoMember(1)]
        public string GameUserId { get; set; } = string.Empty;

        [ProtoMember(2)]
        public int RoomId { get; set; }

        [ProtoMember(3)]
        public double CurrentPayout { get; set; }

        [ProtoMember(4)]
        public long UserTotalBetAmount { get; set; }

        [ProtoMember(5)]
        public double JackpotProb { get; set; }
    }
}
