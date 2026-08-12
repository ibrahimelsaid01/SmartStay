namespace SmartStayBLL
{
    public sealed class PropertyCapacityResponse
    {
        public Guid Id { get; set; }

        public int MaxGuests { get; set; }

        public int Bedrooms { get; set; }

        public int Beds { get; set; }

        public decimal Bathrooms { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}