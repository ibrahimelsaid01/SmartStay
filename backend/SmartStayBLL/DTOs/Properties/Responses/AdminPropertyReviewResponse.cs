namespace SmartStayBLL
{
    public sealed class AdminPropertyReviewResponse
    {
        public Guid Id { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset ReviewedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        public string? RejectionReason { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}