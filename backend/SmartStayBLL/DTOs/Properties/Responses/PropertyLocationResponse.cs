namespace SmartStayBLL
{
    public sealed class PropertyLocationResponse
    {
        public Guid Id { get; set; }

        public string Country { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string StreetAddress { get; set; } =
            string.Empty;

        public string? BuildingNumber { get; set; }

        public string? Floor { get; set; }

        public string? ApartmentNumber { get; set; }

        public string? PostalCode { get; set; }

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}