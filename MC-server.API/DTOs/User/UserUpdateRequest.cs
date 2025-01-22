namespace MC_server.API.DTOs.User
{
    public class UserUpdateRequest
    {
        public string? Nickname { get; set; }
        public int? AddCoins { get; set; }
        public int? Level { get; set; }
        public long? Experience { get; set; }

        // 허용되지 않은 필드를 확인하는 메서드
        public List<string> GetInvalidKeys(IEnumerable<string> allowedKeys)
        {
            var requestKeys = this.GetType().GetProperties()
                .Where(p => p.GetValue(this) != null)
                .Select(p => p.Name.ToLower())
                .ToList();

            return requestKeys.Except(allowedKeys.Select(k => k.ToLower())).ToList();
        }
    }
}
