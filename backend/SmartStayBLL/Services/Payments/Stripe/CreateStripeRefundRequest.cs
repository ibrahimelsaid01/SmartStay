namespace SmartStayBLL
{
    public sealed class CreateStripeRefundRequest
    {
        /*
         * Local refund operation id.
         */
        public Guid RefundId { get; set; }

        /*
         * Local BookingPayment id.
         */
        public Guid PaymentId { get; set; }

        public Guid BookingId { get; set; }

        public Guid GuestUserId { get; set; }

        /*
         * Stripe PaymentIntent id.
         *
         * Example:
         * pi_...
         */
        public string ProviderPaymentId { get; set; } =
            string.Empty;

        /*
         * Refund amount in major currency unit.
         *
         * Example:
         * 500.00 EGP
         */
        public decimal Amount { get; set; }

        public string Currency { get; set; } =
            "EGP";

        /*
         * SmartStay-generated idempotency key.
         *
         * The same key must return the same Stripe refund
         * instead of creating duplicate refunds.
         */
        public string ProviderIdempotencyKey { get; set; } =
            string.Empty;
    }
}