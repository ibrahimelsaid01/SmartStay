namespace SmartStayBLL
{
    public sealed class AuthResult
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTimeOffset AccessTokenExpiresAt { get; set; }

        public string RefreshToken { get; set; } = string.Empty;

        public DateTimeOffset RefreshTokenExpiresAt { get; set; }

        public bool IsNewUser { get; set; }

        public string NextStep { get; set; } = string.Empty;

        public AuthenticatedUserResponse User { get; set; } = null!;
    }
}