namespace SmartStayBLL
{
    public sealed class HostBookingDetailsResponse
    {
        public Guid BookingId { get; set; }

        public HostBookingPropertyResponse Property { get; set; } =
            new();

        public HostBookingGuestResponse Guest { get; set; } =
            new();

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

        /*
         * Comes from CancellationPolicySnapshot,
         * not from the property's current policy.
         */
        public string CancellationPolicy { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public bool IsUpcoming { get; set; }

        public bool IsCurrentlyStaying { get; set; }

        public bool IsPaymentWindowExpired { get; set; }

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