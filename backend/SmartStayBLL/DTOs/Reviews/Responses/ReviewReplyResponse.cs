namespace SmartStayBLL
{
    public sealed class ReviewReplyResponse
    {
        public Guid Id { get; set; }

        public Guid HostProfileId { get; set; }

        public string HostDisplayName { get; set; } =
            string.Empty;

        public string? HostProfileImageUrl { get; set; }

        public string Content { get; set; } =
            string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}