using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class NotificationPublisher
        : INotificationPublisher
    {
        private const int MaximumTitleLength =
            160;

        private const int MaximumMessageLength =
            1000;

        private const int MaximumDeduplicationKeyLength =
            200;

        private readonly SmartStayDbContext _dbContext;

        public NotificationPublisher(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        public async Task PublishAsync(
            NotificationPublishRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ValidateRequest(
                request);

            var title =
                NormalizeRequiredText(
                    request.Title,
                    MaximumTitleLength,
                    "The notification title");

            var message =
                NormalizeRequiredText(
                    request.Message,
                    MaximumMessageLength,
                    "The notification message");

            var deduplicationKey =
                NormalizeOptionalDeduplicationKey(
                    request.DeduplicationKey);

            /*
             * Notifications are not created for inactive
             * accounts because those users cannot access
             * the notification inbox.
             */
            var recipient =
                await _dbContext.Users
                    .AsNoTracking()
                    .Where(user =>
                        user.Id == request.UserId)
                    .Select(user =>
                        new
                        {
                            user.IsActive
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (recipient is null)
            {
                throw new KeyNotFoundException(
                    "The notification recipient was not found.");
            }

            if (!recipient.IsActive)
            {
                return;
            }

            /*
             * Fast idempotency check before attempting
             * the INSERT.
             */
            if (deduplicationKey is not null)
            {
                var duplicateExists =
                    await _dbContext.Notifications
                        .AsNoTracking()
                        .AnyAsync(
                            notification =>
                                notification.UserId ==
                                    request.UserId
                                &&
                                notification
                                    .DeduplicationKey ==
                                    deduplicationKey,
                            cancellationToken);

                if (duplicateExists)
                {
                    return;
                }
            }

            var notification =
                new Notification
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        request.UserId,

                    Type =
                        request.Type,

                    Title =
                        title,

                    Message =
                        message,

                    ReferenceType =
                        request.ReferenceType,

                    ReferenceId =
                        request.ReferenceId,

                    DeduplicationKey =
                        deduplicationKey,

                    CreatedAt =
                        DateTimeOffset.UtcNow,

                    ReadAt =
                        null
                };

            _dbContext.Notifications.Add(
                notification);

            try
            {
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException)
                when (deduplicationKey is not null)
            {
                /*
                 * Two identical requests could pass the
                 * initial check simultaneously.
                 *
                 * The filtered unique index remains the
                 * final concurrency protection.
                 */
                _dbContext.Entry(notification).State =
                    EntityState.Detached;

                var duplicateNowExists =
                    await _dbContext.Notifications
                        .AsNoTracking()
                        .AnyAsync(
                            item =>
                                item.UserId ==
                                    request.UserId
                                &&
                                item.DeduplicationKey ==
                                    deduplicationKey,
                            cancellationToken);

                if (!duplicateNowExists)
                {
                    throw;
                }
            }
        }

        private static void ValidateRequest(
            NotificationPublishRequest request)
        {
            if (request.UserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The notification recipient identifier is invalid.");
            }

            if (!Enum.IsDefined(
                    typeof(NotificationType),
                    request.Type))
            {
                throw new ArgumentException(
                    "The notification type is invalid.");
            }

            if (!Enum.IsDefined(
                    typeof(NotificationReferenceType),
                    request.ReferenceType))
            {
                throw new ArgumentException(
                    "The notification reference type is invalid.");
            }

            var hasReference =
                request.ReferenceType !=
                    NotificationReferenceType.None;

            if (!hasReference
                &&
                request.ReferenceId.HasValue)
            {
                throw new ArgumentException(
                    "A notification without a reference type cannot contain a reference identifier.");
            }

            if (hasReference
                &&
                (!request.ReferenceId.HasValue
                 ||
                 request.ReferenceId.Value ==
                    Guid.Empty))
            {
                throw new ArgumentException(
                    "The notification reference identifier is required.");
            }
        }

        private static string NormalizeRequiredText(
            string? value,
            int maximumLength,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    $"{fieldName} is required.");
            }

            var normalizedValue =
                value.Trim();

            if (normalizedValue.Length >
                maximumLength)
            {
                throw new ArgumentException(
                    $"{fieldName} cannot exceed " +
                    $"{maximumLength} characters.");
            }

            return normalizedValue;
        }

        private static string?
            NormalizeOptionalDeduplicationKey(
                string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            var normalizedValue =
                value.Trim()
                    .ToLowerInvariant();

            if (normalizedValue.Length >
                MaximumDeduplicationKeyLength)
            {
                throw new ArgumentException(
                    "The notification deduplication key " +
                    $"cannot exceed " +
                    $"{MaximumDeduplicationKeyLength} characters.");
            }

            return normalizedValue;
        }
    }
}