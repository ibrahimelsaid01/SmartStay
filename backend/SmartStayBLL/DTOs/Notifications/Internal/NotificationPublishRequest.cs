using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class NotificationPublishRequest
    {
        public Guid UserId { get; set; }

        public NotificationType Type { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string Message { get; set; } =
            string.Empty;

        public NotificationReferenceType ReferenceType
        { get; set; } =
            NotificationReferenceType.None;

        public Guid? ReferenceId { get; set; }

        public string? DeduplicationKey { get; set; }
    }
}