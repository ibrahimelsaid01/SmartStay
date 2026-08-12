namespace SmartStayBLL
{
    public sealed class AccessTokenResult
    {
        public string Token { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }
    }
}