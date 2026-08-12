namespace SmartStayBLL
{
    public sealed class StripeRefundResult
    {
        public string RefundId { get; set; } =
            string.Empty;

        public string? PaymentIntentId { get; set; }

        public long AmountInMinorUnit { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        /*
         * Stripe raw refund status:
         *
         * pending
         * requires_action
         * succeeded
         * failed
         * canceled
         */
        public string Status { get; set; } =
            string.Empty;

        public string? FailureReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}