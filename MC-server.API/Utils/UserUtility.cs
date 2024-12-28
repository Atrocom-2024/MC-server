namespace MC_server.API.Utils
{
    public static class UserUtility
    {
        public static string GenerateRandomNickname()
        {
            string prefix = "user";
            string randomNumbers = new Random().Next(0, 1_000_000_000).ToString();
            return $"{prefix}{randomNumbers}";
        }
    }
}
