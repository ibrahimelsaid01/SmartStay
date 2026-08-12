namespace SmartStayBLL
{
    public sealed class GuestBookingDetailsResponse
    {
        public Guid BookingId { get; set; }

        public GuestBookingPropertyResponse Property { get; set; } =
            new();

        public Guid GuestUserId { get; set; }

        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        public int GuestsCount { get; set; }

        public int Nights { get; set; }

        public decimal PricePerNight { get; set; }

        public decimal Subtotal { get; set; }

        public decimal ServiceFee { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string CancellationPolicy { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public bool CanCancel { get; set; }

        /*
         * True when:
         *
         * Status = Expired
         *
         * or:
         *
         * Status = Pending
         * and ExpiresAt <= current UTC time.
         */
        public bool IsPaymentWindowExpired { get; set; }

        /*
         * Refund values apply only to Confirmed bookings.
         *
         * Pending bookings have no confirmed payment yet,
         * so their refund values remain zero.
         */
        public decimal EstimatedRefundPercentage { get; set; }

        public decimal EstimatedRefundAmount { get; set; }

        public string? CancellationReason { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }

        public DateTimeOffset? CancelledAt { get; set; }

        public DateTimeOffset? ExpiredAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }
    }
}