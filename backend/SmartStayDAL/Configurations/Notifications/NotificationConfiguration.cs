using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class NotificationConfiguration
        : IEntityTypeConfiguration<Notification>
    {
        public void Configure(
            EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable(
                "Notifications",
                tableBuilder =>
                {
                    /*
                     * NotificationType values:
                     * 1 through 16.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Notifications_Type_Valid",
                        "[Type] BETWEEN 1 AND 16");

                    /*
                     * NotificationReferenceType values:
                     *
                     * 0 = None
                     * 1 = Booking
                     * 2 = Payment
                     * 3 = Property
                     * 4 = HostApplication
                     * 5 = Review
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Notifications_ReferenceType_Valid",
                        "[ReferenceType] BETWEEN 0 AND 5");

                    /*
                     * None requires a null ReferenceId.
                     *
                     * Any other reference type requires
                     * a non-null ReferenceId.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Notifications_Reference_Valid",
                        "(" +
                        "[ReferenceType] = 0 " +
                        "AND [ReferenceId] IS NULL" +
                        ") OR (" +
                        "[ReferenceType] <> 0 " +
                        "AND [ReferenceId] IS NOT NULL" +
                        ")");

                    /*
                     * A notification cannot be marked as read
                     * before it was created.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Notifications_ReadAt_Valid",
                        "[ReadAt] IS NULL " +
                        "OR [ReadAt] >= [CreatedAt]");
                });

            builder.HasKey(notification =>
                notification.Id);

            builder.Property(notification =>
                    notification.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(notification =>
                    notification.Title)
                .IsRequired()
                .HasMaxLength(160);

            builder.Property(notification =>
                    notification.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(notification =>
                    notification.ReferenceType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(notification =>
                    notification.ReferenceId)
                .IsRequired(false);

            builder.Property(notification =>
                    notification.DeduplicationKey)
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(notification =>
                    notification.CreatedAt)
                .IsRequired();

            builder.Property(notification =>
                    notification.ReadAt)
                .IsRequired(false);

            /*
             * Notifications are private to one user.
             *
             * If a user is ever physically deleted,
             * their notifications are deleted too.
             *
             * SmartStay normally uses account deactivation
             * instead of hard deletion.
             */
            builder.HasOne(notification =>
                    notification.User)
                .WithMany()
                .HasForeignKey(notification =>
                    notification.UserId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            /*
             * Main inbox query:
             *
             * Get notifications for a user and order them
             * from newest to oldest.
             */
            builder.HasIndex(notification =>
                    new
                    {
                        notification.UserId,
                        notification.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_Notifications_User_CreatedAt");

            /*
             * Used for unread-count and unread-list queries.
             */
            builder.HasIndex(notification =>
                    new
                    {
                        notification.UserId,
                        notification.ReadAt,
                        notification.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_Notifications_User_ReadAt_CreatedAt");

            /*
             * Prevents duplicate notifications for the
             * same user and domain event.
             *
             * Null keys are excluded from the unique index.
             */
            builder.HasIndex(notification =>
                    new
                    {
                        notification.UserId,
                        notification.DeduplicationKey
                    })
                .IsUnique()
                .HasFilter(
                    "[DeduplicationKey] IS NOT NULL")
                .HasDatabaseName(
                    "IX_Notifications_User_DeduplicationKey_Unique");

            /*
             * Useful for finding notifications related to
             * a booking, property, review, or payment.
             */
            builder.HasIndex(notification =>
                    new
                    {
                        notification.ReferenceType,
                        notification.ReferenceId
                    })
                .HasDatabaseName(
                    "IX_Notifications_Reference");
        }
    }
}