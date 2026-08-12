namespace SmartStayBLL
{
    public sealed class StripeSettings
    {
        public const string SectionName =
            "Stripe";

        /*
         * Server-side secret key.
         *
         * Example:
         * sk_test_...
         *
         * Must never be exposed to the frontend.
         */
        public string SecretKey { get; set; } =
            string.Empty;

        /*
         * Public key used by the frontend.
         *
         * Example:
         * pk_test_...
         */
        public string PublishableKey { get; set; } =
            string.Empty;

        /*
         * Stripe webhook signing secret.
         *
         * Example:
         * whsec_...
         *
         * It will be used when we implement
         * the Stripe webhook endpoint.
         */
        public string WebhookSecret { get; set; } =
            string.Empty;
    }
}