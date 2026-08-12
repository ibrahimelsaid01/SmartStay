namespace SmartStayBLL
{
    public sealed class NotificationResponse
    {
        public Guid Id { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public string Title { get; set; } =
            string.Empty;

        public string Message { get; set; } =
            string.Empty;

        public string ReferenceType { get; set; } =
            string.Empty;

        public Guid? ReferenceId { get; set; }

        public bool IsRead { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? ReadAt { get; set; }
    }
}