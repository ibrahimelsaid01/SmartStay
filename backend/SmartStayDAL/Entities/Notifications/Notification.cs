namespace SmartStayDAL
{
    public sealed class Notification
    {
        public Guid Id { get; set; }

        /*
         * The user who will receive the notification.
         */
        public Guid UserId { get; set; }

        public NotificationType Type { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string Message { get; set; } =
            string.Empty;

        /*
         * Identifies the domain object related to
         * the notification.
         *
         * Examples:
         * Booking, Property, Review.
         */
        public NotificationReferenceType ReferenceType
        { get; set; } =
            NotificationReferenceType.None;

        public Guid? ReferenceId { get; set; }

        /*
         * Used to prevent duplicate notifications.
         *
         * Examples:
         *
         * booking-confirmed:{bookingId}
         * property-published:{propertyId}
         * review-approved:{reviewId}
         *
         * The database guarantees that the same user cannot
         * receive two notifications with the same key.
         */
        public string? DeduplicationKey { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        /*
         * Null means the notification is unread.
         */
        public DateTimeOffset? ReadAt { get; set; }

        public ApplicationUser User { get; set; } =
            null!;
    }
}