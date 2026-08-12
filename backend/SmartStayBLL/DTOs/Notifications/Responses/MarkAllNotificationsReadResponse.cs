namespace SmartStayBLL
{
    public sealed class MarkAllNotificationsReadResponse
    {
        public int UpdatedCount { get; set; }

        public DateTimeOffset ReadAt { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}