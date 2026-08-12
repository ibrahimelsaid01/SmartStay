namespace SmartStayBLL
{
    public sealed class OtpCooldownException : Exception
    {
        public int RetryAfterSeconds { get; }

        public OtpCooldownException(
            int retryAfterSeconds)
            : base(
                $"Please wait {retryAfterSeconds} seconds before requesting another code.")
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}