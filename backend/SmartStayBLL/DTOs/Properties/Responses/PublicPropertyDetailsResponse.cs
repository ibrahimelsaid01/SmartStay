namespace SmartStayBLL
{
    public sealed class PublicPropertyDetailsResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        public string PropertyType { get; set; } =
            string.Empty;

        public string SpaceType { get; set; } =
            string.Empty;

        public string Country { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string StreetAddress { get; set; } =
            string.Empty;

        public string? PostalCode { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string FullAddress { get; set; } =
            string.Empty;

        public int MaxGuests { get; set; }

        public int Bedrooms { get; set; }

        public int Beds { get; set; }

        public decimal Bathrooms { get; set; }

        public decimal PricePerNight { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public decimal AverageRating { get; set; }

        public int ReviewsCount { get; set; }

        public TimeOnly CheckInTime { get; set; }

        public TimeOnly CheckOutTime { get; set; }

        public string CancellationPolicy { get; set; } =
            string.Empty;

        public bool AllowsSmoking { get; set; }

        public bool AllowsPets { get; set; }

        public bool AllowsParties { get; set; }

        public bool AllowsChildren { get; set; }

        public string? AdditionalHouseRules { get; set; }

        public PublicPropertyHostResponse Host { get; set; } =
            new();

        public IReadOnlyList<PublicPropertyImageResponse>
            Images
        { get; set; } =
            Array.Empty<PublicPropertyImageResponse>();

        public IReadOnlyList<PublicPropertyAmenityResponse>
            Amenities
        { get; set; } =
            Array.Empty<PublicPropertyAmenityResponse>();

        public DateTimeOffset? PublishedAt { get; set; }
    }
}