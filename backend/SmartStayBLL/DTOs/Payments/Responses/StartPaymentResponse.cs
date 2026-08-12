namespace SmartStayBLL
{
    public sealed class StartPaymentResponse
    {
        public Guid PaymentId { get; set; }

        public Guid BookingId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string Provider { get; set; } =
            "STRIPE";

        public string ProviderPaymentId { get; set; } =
            string.Empty;

        /*
         * Used only by Stripe.js / Stripe Elements.
         *
         * Never write this value into logs.
         */
        public string ClientSecret { get; set; } =
            string.Empty;

        /*
         * Local SmartStay payment status.
         */
        public string Status { get; set; } =
            string.Empty;

        /*
         * Stripe PaymentIntent status.
         *
         * Example:
         * requires_payment_method
         * requires_action
         * processing
         * succeeded
         */
        public string ProviderStatus { get; set; } =
            string.Empty;

        public DateTimeOffset? BookingExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public bool WasAlreadyProcessed { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}