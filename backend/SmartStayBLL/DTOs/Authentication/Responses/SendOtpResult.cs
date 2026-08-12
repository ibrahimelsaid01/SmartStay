namespace SmartStayBLL
{
    public sealed class SendOtpResult
    {
        public int ResendAvailableAfterSeconds { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }
    }
}