namespace SmartStayDAL
{
    public class OtpCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string NormalizedEmail { get; set; } = string.Empty;

        public string CodeHash { get; set; } = string.Empty;

        public OtpPurpose Purpose { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public DateTimeOffset? UsedAt { get; set; }

        public DateTimeOffset? InvalidatedAt { get; set; }

        public int FailedAttempts { get; set; }

        public Guid? UserId { get; set; }

        public ApplicationUser? User { get; set; }
    }
}