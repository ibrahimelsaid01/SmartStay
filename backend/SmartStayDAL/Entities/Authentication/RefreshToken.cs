namespace SmartStayDAL
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string TokenHash { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public string? CreatedByIp { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }

        public string? RevokedByIp { get; set; }

        public string? RevocationReason { get; set; }

        public Guid? ReplacedByTokenId { get; set; }

        public RefreshToken? ReplacedByToken { get; set; }

        public Guid UserId { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}