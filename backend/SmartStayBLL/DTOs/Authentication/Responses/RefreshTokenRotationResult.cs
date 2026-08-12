namespace SmartStayBLL
{
    public sealed class RefreshTokenRotationResult
    {
        public Guid UserId { get; set; }

        public string Token { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }
    }
}