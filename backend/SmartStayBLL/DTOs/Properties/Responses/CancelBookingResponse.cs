namespace SmartStayBLL
{
    public sealed class CancelBookingResponse
    {
        public Guid BookingId { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string CancellationPolicy { get; set; } =
            string.Empty;

        public decimal EstimatedRefundPercentage { get; set; }

        public decimal EstimatedRefundAmount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string? CancellationReason { get; set; }

        public DateTimeOffset CancelledAt { get; set; }

        /*
         * True only when the confirmed booking has
         * a refundable amount greater than zero.
         */
        public bool IsRefundRequired { get; set; }

        /*
         * The refund operation created by SmartStay.
         */
        public Guid? RefundId { get; set; }

        /*
         * Stripe refund id.
         *
         * Example:
         * re_...
         */
        public string? ProviderRefundId { get; set; }

        /*
         * Pending, RequiresAction, Succeeded,
         * Failed, or Cancelled.
         */
        public string? RefundStatus { get; set; }

        public decimal RefundAmount { get; set; }

        public string? RefundMessage { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}