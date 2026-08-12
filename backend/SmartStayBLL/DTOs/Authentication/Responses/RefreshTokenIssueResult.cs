namespace SmartStayBLL
{
    public sealed class RefreshTokenIssueResult
    {
        public string Token { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }
    }
}