namespace SmartStayBLL
{
    public sealed class OtpSettings
    {
        public const string SectionName = "OtpSettings";

        public int CodeLength { get; set; } = 6;

        public int ExpirationMinutes { get; set; } = 10;

        public int ResendCooldownSeconds { get; set; } = 30;

        public int MaximumFailedAttempts { get; set; } = 5;

        public string HashKey { get; set; } = string.Empty;
    }
}