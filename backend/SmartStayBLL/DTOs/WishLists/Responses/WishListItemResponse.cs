namespace SmartStayBLL
{
    public sealed class WishListItemResponse
    {
        public Guid PropertyId { get; set; }

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

        public decimal? PricePerNight { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string? CoverImageUrl { get; set; }

        public int? MaxGuests { get; set; }

        public decimal AverageRating { get; set; }

        public int ReviewsCount { get; set; }

        public bool IsAvailable { get; set; }

        public string? Note { get; set; }

        public DateTimeOffset AddedAt { get; set; }
    }
}