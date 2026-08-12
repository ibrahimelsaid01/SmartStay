namespace SmartStayBLL
{
    public sealed class GuestBookingListItemResponse
    {
        public Guid BookingId { get; set; }

        public GuestBookingPropertyResponse Property { get; set; } =
            new();

        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        public int Nights { get; set; }

        public int GuestsCount { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        /*
         * True only when the booking can currently
         * be cancelled by the guest.
         */
        public bool CanCancel { get; set; }

        /*
         * True only when:
         *
         * - The booking status is Completed.
         * - The guest has not already created
         *   a review for this booking.
         */
        public bool CanReview { get; set; }

        /*
         * True when a review already exists
         * for this booking.
         */
        public bool HasReview { get; set; }

        /*
         * The existing review identifier.
         *
         * Null when the guest has not created
         * a review for this booking yet.
         */
        public Guid? ReviewId { get; set; }

        /*
         * The current moderation status of the
         * existing review:
         *
         * Pending
         * Posted
         * Rejected
         *
         * Null when no review exists.
         */
        public string? ReviewStatus { get; set; }

        /*
         * Can become true while the database status is
         * temporarily still Pending, before the lifecycle
         * background service changes it to Expired.
         */
        public bool IsPaymentWindowExpired { get; set; }

        /*
         * Relevant mainly while the booking is Pending.
         */
        public DateTimeOffset? ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }

        public DateTimeOffset? CancelledAt { get; set; }

        public DateTimeOffset? ExpiredAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }
    }
}