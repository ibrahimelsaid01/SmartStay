namespace SmartStayBLL
{
    public interface INotificationPublisher
    {
        /*
         * Creates an in-app notification.
         *
         * The operation is idempotent when a
         * DeduplicationKey is provided.
         */
        Task PublishAsync(
            NotificationPublishRequest request,
            CancellationToken cancellationToken = default);
    }
}