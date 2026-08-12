namespace SmartStayDAL
{
    public sealed class Booking
    {
        public Guid Id { get; set; }

        public Guid PropertyId { get; set; }

        public Guid GuestUserId { get; set; }

        /*
         * Check-in is inclusive.
         * Check-out is exclusive.
         */
        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        public int GuestsCount { get; set; }

        public int Nights { get; set; }

        /*
         * Pricing snapshot.
         *
         * Future changes to the property price must not
         * affect this booking.
         */
        public decimal PricePerNight { get; set; }

        public decimal Subtotal { get; set; }

        public decimal ServiceFee { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; } =
            "EGP";

        /*
         * Cancellation policy snapshot.
         *
         * Future changes to the property's policy must not
         * affect this booking.
         */
        public CancellationPolicyType
            CancellationPolicySnapshot
        { get; set; } =
                CancellationPolicyType.Moderate;

        /*
         * Booking terms acceptance.
         *
         * These values prove that the guest accepted the
         * booking terms before the booking was created.
         */
        public bool AcceptedBookingTerms { get; set; }

        public bool AcceptedCancellationPolicy { get; set; }

        public bool AcceptedPropertyRules { get; set; }

        public bool AcceptedComplaintPolicy { get; set; }

        public DateTimeOffset? BookingTermsAcceptedAt { get; set; }

        public BookingStatus Status { get; set; }

        public string? CancellationReason { get; set; }

        /*
         * The time at which a Pending booking stops
         * reserving the selected dates if payment has
         * not been confirmed.
         */
        public DateTimeOffset? ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        /*
         * Set when payment succeeds and the booking
         * transitions from Pending to Confirmed.
         */
        public DateTimeOffset? ConfirmedAt { get; set; }

        /*
         * Set when the guest cancels the booking.
         */
        public DateTimeOffset? CancelledAt { get; set; }

        /*
         * Set when the system expires a Pending booking.
         */
        public DateTimeOffset? ExpiredAt { get; set; }

        /*
         * Set when the stay reaches or passes
         * its check-out date.
         */
        public DateTimeOffset? CompletedAt { get; set; }

        public Property Property { get; set; } =
            null!;

        public ApplicationUser GuestUser { get; set; } =
            null!;
    }
}