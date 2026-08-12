namespace SmartStayBLL
{
    public sealed class HostPropertyUnpublishResponse
    {
        public Guid Id { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? PublishedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}