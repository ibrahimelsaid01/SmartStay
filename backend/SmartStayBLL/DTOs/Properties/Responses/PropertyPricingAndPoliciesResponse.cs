namespace SmartStayBLL
{
    public sealed class
        PropertyPricingAndPoliciesResponse
    {
        public Guid Id { get; set; }

        public decimal PricePerNight { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public TimeOnly CheckInTime { get; set; }

        public TimeOnly CheckOutTime { get; set; }

        public string CancellationPolicy { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}