namespace SmartStayBLL
{
    public sealed class NotificationsResponse
    {
        public IReadOnlyList<NotificationResponse> Items
        { get; set; } =
            Array.Empty<NotificationResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public int UnreadCount { get; set; }
    }
}