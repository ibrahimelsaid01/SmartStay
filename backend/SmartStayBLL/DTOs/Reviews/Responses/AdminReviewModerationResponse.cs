namespace SmartStayBLL
{
    public sealed class AdminReviewModerationResponse
    {
        public Guid Id { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset ModeratedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        public DateTimeOffset? RejectedAt { get; set; }

        public string? RejectionReason { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}