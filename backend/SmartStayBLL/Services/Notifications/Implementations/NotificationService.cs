using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class NotificationService
        : INotificationService
    {
        private const int MaximumPageSize = 100;

        private readonly SmartStayDbContext _dbContext;

        public NotificationService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        // =====================================================
        // Get notification inbox
        // =====================================================

        public async Task<NotificationsResponse>
            GetNotificationsAsync(
                Guid userId,
                bool unreadOnly,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                userId);

            ValidatePagination(
                page,
                pageSize);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var query =
                _dbContext.Notifications
                    .AsNoTracking()
                    .Where(notification =>
                        notification.UserId == userId);

            if (unreadOnly)
            {
                query =
                    query.Where(notification =>
                        notification.ReadAt == null);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var unreadCount =
                await _dbContext.Notifications
                    .AsNoTracking()
                    .CountAsync(
                        notification =>
                            notification.UserId == userId
                            &&
                            notification.ReadAt == null,
                        cancellationToken);

            var projections =
                await query
                    .OrderByDescending(notification =>
                        notification.CreatedAt)
                    .ThenByDescending(notification =>
                        notification.Id)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(notification =>
                        new NotificationProjection
                        {
                            Id =
                                notification.Id,

                            Type =
                                notification.Type,

                            Title =
                                notification.Title,

                            Message =
                                notification.Message,

                            ReferenceType =
                                notification.ReferenceType,

                            ReferenceId =
                                notification.ReferenceId,

                            CreatedAt =
                                notification.CreatedAt,

                            ReadAt =
                                notification.ReadAt
                        })
                    .ToListAsync(
                        cancellationToken);

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount
                        /
                        (double)pageSize);

            return new NotificationsResponse
            {
                Items =
                    projections
                        .Select(MapResponse)
                        .ToList(),

                Page =
                    page,

                PageSize =
                    pageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    totalPages,

                UnreadCount =
                    unreadCount
            };
        }

        // =====================================================
        // Get unread count
        // =====================================================

        public async Task<UnreadNotificationsCountResponse>
            GetUnreadCountAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                userId);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var unreadCount =
                await _dbContext.Notifications
                    .AsNoTracking()
                    .CountAsync(
                        notification =>
                            notification.UserId == userId
                            &&
                            notification.ReadAt == null,
                        cancellationToken);

            return new UnreadNotificationsCountResponse
            {
                UnreadCount =
                    unreadCount
            };
        }

        // =====================================================
        // Mark one notification as read
        // =====================================================

        public async Task<NotificationResponse>
            MarkAsReadAsync(
                Guid userId,
                Guid notificationId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                userId);

            ValidateNotificationId(
                notificationId);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var notification =
                await _dbContext.Notifications
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == notificationId
                            &&
                            item.UserId == userId,
                        cancellationToken);

            if (notification is null)
            {
                throw new KeyNotFoundException(
                    "The notification was not found.");
            }

            /*
             * The operation is idempotent.
             *
             * Repeating it does not replace the original
             * ReadAt value.
             */
            if (notification.ReadAt is null)
            {
                notification.ReadAt =
                    DateTimeOffset.UtcNow;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            return MapResponse(
                notification);
        }

        // =====================================================
        // Mark all notifications as read
        // =====================================================

        public async Task<MarkAllNotificationsReadResponse>
            MarkAllAsReadAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                userId);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            /*
             * One direct SQL UPDATE is executed without
             * loading all notifications into application
             * memory.
             */
            var updatedCount =
                await _dbContext.Notifications
                    .Where(notification =>
                        notification.UserId == userId
                        &&
                        notification.ReadAt == null)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                notification =>
                                    notification.ReadAt,
                                currentTime),
                        cancellationToken);

            return new MarkAllNotificationsReadResponse
            {
                UpdatedCount =
                    updatedCount,

                ReadAt =
                    currentTime,

                Message =
                    updatedCount == 0
                        ? "There were no unread notifications."
                        : "All notifications were marked as read."
            };
        }

        // =====================================================
        // Delete one notification
        // =====================================================

        public async Task DeleteAsync(
            Guid userId,
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                userId);

            ValidateNotificationId(
                notificationId);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var notification =
                await _dbContext.Notifications
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == notificationId
                            &&
                            item.UserId == userId,
                        cancellationToken);

            if (notification is null)
            {
                throw new KeyNotFoundException(
                    "The notification was not found.");
            }

            _dbContext.Notifications.Remove(
                notification);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        // =====================================================
        // Delete all user notifications
        // =====================================================

        public async Task<DeleteAllNotificationsResponse>
            DeleteAllAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                userId);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var deletedAt =
                DateTimeOffset.UtcNow;

            /*
             * ExecuteDeleteAsync sends one direct SQL DELETE.
             *
             * Notifications are not loaded into application
             * memory, and notifications belonging to other
             * users cannot be affected.
             */
            var deletedCount =
                await _dbContext.Notifications
                    .Where(notification =>
                        notification.UserId == userId)
                    .ExecuteDeleteAsync(
                        cancellationToken);

            return new DeleteAllNotificationsResponse
            {
                DeletedCount =
                    deletedCount,

                DeletedAt =
                    deletedAt,

                Message =
                    deletedCount == 0
                        ? "There were no notifications to delete."
                        : "All notifications were deleted successfully."
            };
        }

        // =====================================================
        // Mapping
        // =====================================================

        private static NotificationResponse MapResponse(
            Notification notification)
        {
            return new NotificationResponse
            {
                Id =
                    notification.Id,

                Type =
                    notification.Type.ToString(),

                Title =
                    notification.Title,

                Message =
                    notification.Message,

                ReferenceType =
                    notification.ReferenceType.ToString(),

                ReferenceId =
                    notification.ReferenceId,

                IsRead =
                    notification.ReadAt.HasValue,

                CreatedAt =
                    notification.CreatedAt,

                ReadAt =
                    notification.ReadAt
            };
        }

        private static NotificationResponse MapResponse(
            NotificationProjection notification)
        {
            return new NotificationResponse
            {
                Id =
                    notification.Id,

                Type =
                    notification.Type.ToString(),

                Title =
                    notification.Title,

                Message =
                    notification.Message,

                ReferenceType =
                    notification.ReferenceType.ToString(),

                ReferenceId =
                    notification.ReferenceId,

                IsRead =
                    notification.ReadAt.HasValue,

                CreatedAt =
                    notification.CreatedAt,

                ReadAt =
                    notification.ReadAt
            };
        }

        // =====================================================
        // User validation
        // =====================================================

        private async Task EnsureActiveUserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var user =
                await _dbContext.Users
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == userId)
                    .Select(item =>
                        new
                        {
                            item.IsActive
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "The user was not found.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }
        }

        // =====================================================
        // Request validation
        // =====================================================

        private static void ValidateUserId(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user identifier is invalid.");
            }
        }

        private static void ValidateNotificationId(
            Guid notificationId)
        {
            if (notificationId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The notification identifier is invalid.");
            }
        }

        private static void ValidatePagination(
            int page,
            int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentException(
                    "Page must be greater than or equal to 1.");
            }

            if (pageSize < 1
                ||
                pageSize > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"Page size must be between 1 and " +
                    $"{MaximumPageSize}.");
            }
        }

        // =====================================================
        // Internal projection
        // =====================================================

        private sealed class NotificationProjection
        {
            public Guid Id { get; set; }

            public NotificationType Type { get; set; }

            public string Title { get; set; } =
                string.Empty;

            public string Message { get; set; } =
                string.Empty;

            public NotificationReferenceType ReferenceType
            { get; set; }

            public Guid? ReferenceId { get; set; }

            public DateTimeOffset CreatedAt { get; set; }

            public DateTimeOffset? ReadAt { get; set; }
        }
    }
}