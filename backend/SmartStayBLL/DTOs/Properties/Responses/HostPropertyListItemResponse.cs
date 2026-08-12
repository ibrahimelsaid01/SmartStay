namespace SmartStayBLL
{
    public sealed class HostPropertyListItemResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string PropertyType { get; set; } =
            string.Empty;

        public string SpaceType { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string? City { get; set; }

        public decimal? PricePerNight { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string? CoverImageUrl { get; set; }

        public int ImagesCount { get; set; }

        public bool CanEdit { get; set; }

        public bool CanUnpublish { get; set; }

        public string? RejectionReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }
    }
}