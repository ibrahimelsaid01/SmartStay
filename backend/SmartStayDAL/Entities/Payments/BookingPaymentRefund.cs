namespace SmartStayDAL
{
    public sealed class BookingPaymentRefund
    {
        public Guid Id { get; set; }

        /*
         * The successful payment being refunded.
         */
        public Guid BookingPaymentId { get; set; }

        /*
         * The amount requested for this individual
         * refund operation.
         *
         * It may be less than BookingPayment.Amount
         * when the cancellation policy produces a
         * partial refund.
         */
        public decimal Amount { get; set; }

        /*
         * Financial currency snapshot.
         *
         * This must match the original payment currency.
         */
        public string Currency { get; set; } =
            "EGP";

        /*
         * The payment provider processing the refund.
         *
         * Current value:
         *
         * STRIPE
         */
        public string Provider { get; set; } =
            string.Empty;

        /*
         * SmartStay-generated key used when calling Stripe.
         *
         * Repeating the same provider request with the same
         * key must not create another external refund.
         */
        public string IdempotencyKey { get; set; } =
            string.Empty;

        /*
         * Stripe Refund identifier.
         *
         * Example:
         *
         * re_1...
         *
         * It remains null before Stripe successfully
         * creates or returns the Refund object.
         */
        public string? ProviderRefundId { get; set; }

        public PaymentRefundStatus Status { get; set; } =
            PaymentRefundStatus.Pending;

        /*
         * Provider-reported reason when the refund fails.
         *
         * Examples:
         *
         * insufficient_funds
         * declined
         * expired_or_canceled_card
         * unknown
         */
        public string? FailureReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SucceededAt { get; set; }

        public DateTimeOffset? FailedAt { get; set; }

        public DateTimeOffset? CancelledAt { get; set; }

        public BookingPayment BookingPayment { get; set; } =
            null!;
    }
}