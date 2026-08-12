namespace SmartStayBLL
{
    public sealed class GuestBookingConfirmationResponse
    {
        public Guid BookingId { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? ConfirmedAt { get; set; }

        public string GuestEmail { get; set; } =
            string.Empty;

        
        public GuestBookingConfirmationPropertyResponse Property
        { get; set; } = new();

        public GuestBookingConfirmationStayResponse Stay
        { get; set; } = new();

        public GuestBookingConfirmationPricingResponse Pricing
        { get; set; } = new();

        public GuestBookingConfirmationPaymentResponse Payment
        { get; set; } = new();
    }

    public sealed class GuestBookingConfirmationPropertyResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string PropertyType { get; set; } =
            string.Empty;

        public string? CoverImageUrl { get; set; }

        public string Country { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }
        public string? StreetAddress { get; set; }

        public string? BuildingNumber { get; set; }

        public string? Floor { get; set; }

        public string? ApartmentNumber { get; set; }

        public string? PostalCode { get; set; }

        public string FullAddress { get; set; } =
            string.Empty;
    }

    public sealed class GuestBookingConfirmationStayResponse
    {
        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        public int GuestsCount { get; set; }

        public int Nights { get; set; }
    }

    public sealed class GuestBookingConfirmationPricingResponse
    {
        public decimal PricePerNight { get; set; }

        public decimal Subtotal { get; set; }

        public decimal ServiceFee { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; } =
            string.Empty;
    }

    public sealed class GuestBookingConfirmationPaymentResponse
    {
        public Guid PaymentId { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string Provider { get; set; } =
            string.Empty;

        public decimal Amount { get; set; }

        public decimal RefundedAmount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public DateTimeOffset? SucceededAt { get; set; }
    }
}