namespace SmartStayBLL
{
    public sealed class OtpVerificationResult
    {
        public bool IsValid { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }
    }
}