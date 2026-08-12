namespace SmartStayBLL
{
    public sealed class PaymentRefundResponse
    {
        public Guid RefundId { get; set; }

        public Guid PaymentId { get; set; }

        public Guid BookingId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string Provider { get; set; } =
            string.Empty;

        public string? ProviderRefundId { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string? FailureReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SucceededAt { get; set; }

        public DateTimeOffset? FailedAt { get; set; }

        public DateTimeOffset? CancelledAt { get; set; }

        public bool WasAlreadyProcessed { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}