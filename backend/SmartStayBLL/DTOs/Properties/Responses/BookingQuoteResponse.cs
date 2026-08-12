namespace SmartStayBLL
{
    public sealed class BookingQuoteResponse
    {
        public Guid PropertyId { get; set; }

        public string PropertyTitle { get; set; } =
            string.Empty;

        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        public int GuestsCount { get; set; }

        public int Nights { get; set; }

        public decimal PricePerNight { get; set; }

        public decimal Subtotal { get; set; }

        public decimal ServiceFeePercentage { get; set; }

        public decimal ServiceFee { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string CancellationPolicy { get; set; } =
            string.Empty;
    }
}