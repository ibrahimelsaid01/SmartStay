namespace SmartStayBLL
{
    public sealed class DeleteAllNotificationsResponse
    {
        public int DeletedCount { get; set; }

        public DateTimeOffset DeletedAt { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}