namespace SmartStayBLL
{
    public sealed class PublicPropertyListItemResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string PropertyType { get; set; } =
            string.Empty;

        public string SpaceType { get; set; } =
            string.Empty;

        public string Country { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public decimal PricePerNight { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string CoverImageUrl { get; set; } =
            string.Empty;

        public int MaxGuests { get; set; }

        public int Bedrooms { get; set; }

        public int Beds { get; set; }

        public decimal Bathrooms { get; set; }

        public decimal AverageRating { get; set; }

        public int ReviewsCount { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }
    }
}