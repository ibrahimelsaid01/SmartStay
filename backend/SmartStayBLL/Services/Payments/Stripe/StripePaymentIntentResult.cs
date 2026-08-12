namespace SmartStayBLL
{
    public sealed class StripePaymentIntentResult
    {
        public string PaymentIntentId { get; set; } =
            string.Empty;

        public string ClientSecret { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public long AmountInMinorUnit { get; set; }

        public string Currency { get; set; } =
            string.Empty;
    }
}