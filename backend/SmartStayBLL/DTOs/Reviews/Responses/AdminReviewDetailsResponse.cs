namespace SmartStayBLL
{
    public sealed class AdminReviewDetailsResponse
    {
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }

        public int Rating { get; set; }

        public string? PositiveComment { get; set; }

        public string? NegativeComment { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string? RejectionReason { get; set; }

        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        public int HelpfulCount { get; set; }

        public ReviewAuthorResponse Author
        { get; set; } = new();

        public ReviewPropertyResponse Property
        { get; set; } = new();

        public ReviewReplyResponse? Reply
        { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? ModeratedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        public DateTimeOffset? RejectedAt { get; set; }
    }
}