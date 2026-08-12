namespace SmartStayBLL
{
    public sealed class AdminReviewListItemResponse
    {
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }

        public int Rating { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public ReviewAuthorResponse Author
        { get; set; } = new();

        public ReviewPropertyResponse Property
        { get; set; } = new();

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        public DateTimeOffset? RejectedAt { get; set; }
    }
}