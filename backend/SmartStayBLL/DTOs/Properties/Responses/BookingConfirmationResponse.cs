namespace SmartStayBLL
{
    public sealed class BookingConfirmationResponse
    {
        public Guid BookingId { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? ConfirmedAt { get; set; }

        /*
         * True when the same payment confirmation
         * is received more than once.
         *
         * Payment providers may retry webhooks, so the
         * operation must be idempotent.
         */
        public bool WasAlreadyProcessed { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}