namespace SmartStayBLL
{
    public sealed class AdminPendingPropertyItemResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string PropertyType { get; set; } =
            string.Empty;

        public string SpaceType { get; set; } =
            string.Empty;

        public string? City { get; set; }

        public decimal? PricePerNight { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string? CoverImageUrl { get; set; }

        public Guid HostUserId { get; set; }

        public string HostName { get; set; } =
            string.Empty;

        public string HostEmail { get; set; } =
            string.Empty;

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}