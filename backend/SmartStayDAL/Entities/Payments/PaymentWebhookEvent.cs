namespace SmartStayDAL
{
    public sealed class PaymentWebhookEvent
    {
        public Guid Id { get; set; }

        public string Provider { get; set; } =
            string.Empty;

        /*
         * Stripe Event identifier.
         *
         * Example:
         * evt_1...
         */
        public string ProviderEventId { get; set; } =
            string.Empty;

        /*
         * Examples:
         *
         * payment_intent.succeeded
         * payment_intent.payment_failed
         * payment_intent.canceled
         */
        public string EventType { get; set; } =
            string.Empty;

        public DateTimeOffset ReceivedAt { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }
    }
}