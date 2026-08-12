namespace SmartStayBLL
{
    public sealed class PaymentStatusResponse
    {
        public Guid PaymentId { get; set; }

        public Guid BookingId { get; set; }

        public string BookingStatus { get; set; } =
            string.Empty;

        public decimal Amount { get; set; }

        public decimal RefundedAmount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string Provider { get; set; } =
            string.Empty;

        public string? ProviderPaymentId { get; set; }

        public string? ProviderReference { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string? FailureCode { get; set; }

        public string? FailureMessage { get; set; }

        public DateTimeOffset? BookingExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SucceededAt { get; set; }

        public DateTimeOffset? FailedAt { get; set; }

        public DateTimeOffset? CancelledAt { get; set; }

        public DateTimeOffset? RefundedAt { get; set; }

        /*
         * Pending is the only non-final status.
         *
         * Failed and Cancelled are final for the current
         * payment attempt, but the user may start another
         * attempt if the booking is still payable.
         */
        public bool IsFinal { get; set; }
    }
}