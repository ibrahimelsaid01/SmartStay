namespace SmartStayBLL
{
    public interface INotificationService
    {
        Task<NotificationsResponse> GetNotificationsAsync(
            Guid userId,
            bool unreadOnly,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<UnreadNotificationsCountResponse>
            GetUnreadCountAsync(
                Guid userId,
                CancellationToken cancellationToken = default);

        Task<NotificationResponse> MarkAsReadAsync(
            Guid userId,
            Guid notificationId,
            CancellationToken cancellationToken = default);

        Task<MarkAllNotificationsReadResponse>
            MarkAllAsReadAsync(
                Guid userId,
                CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid userId,
            Guid notificationId,
            CancellationToken cancellationToken = default);

        Task<DeleteAllNotificationsResponse>
            DeleteAllAsync(
                Guid userId,
                CancellationToken cancellationToken = default);
    }
}