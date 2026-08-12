namespace SmartStayBLL
{
    public sealed class CreateBookingResponse
    {
        public Guid BookingId { get; set; }

        public Guid PropertyId { get; set; }

        public string PropertyTitle { get; set; } =
            string.Empty;

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

        /*
         * The guest must complete payment before this
         * UTC timestamp.
         */
        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}