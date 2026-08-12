using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class ModerationNotificationInterceptor
        : SaveChangesInterceptor
    {
        public override InterceptionResult<int>
            SavingChanges(
                DbContextEventData eventData,
                InterceptionResult<int> result)
        {
            if (eventData.Context
                is SmartStayDbContext dbContext)
            {
                AddModerationNotifications(
                    dbContext);
            }

            return base.SavingChanges(
                eventData,
                result);
        }

        public override ValueTask<
            InterceptionResult<int>>
            SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
        {
            if (eventData.Context
                is SmartStayDbContext dbContext)
            {
                AddModerationNotifications(
                    dbContext);
            }

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        private static void AddModerationNotifications(
            SmartStayDbContext dbContext)
        {
            dbContext.ChangeTracker
                .DetectChanges();

            /*
             * Materialize entries before adding Notification
             * entities to the ChangeTracker.
             */
            var hostApplicationEntries =
                dbContext.ChangeTracker
                    .Entries<HostProfile>()
                    .Where(
                        IsHostApplicationModerationTransition)
                    .ToList();

            var propertyEntries =
                dbContext.ChangeTracker
                    .Entries<Property>()
                    .Where(
                        IsPropertyModerationTransition)
                    .ToList();

            if (hostApplicationEntries.Count == 0
                &&
                propertyEntries.Count == 0)
            {
                return;
            }

            /*
             * Protects against adding the same notification
             * more than once when SaveChanges is retried on
             * the same DbContext instance.
             */
            var trackedNotificationKeys =
                dbContext.ChangeTracker
                    .Entries<Notification>()
                    .Where(entry =>
                        entry.State ==
                            EntityState.Added
                        &&
                        !string.IsNullOrWhiteSpace(
                            entry.Entity
                                .DeduplicationKey))
                    .Select(entry =>
                        BuildTrackedKey(
                            entry.Entity.UserId,
                            entry.Entity
                                .DeduplicationKey!))
                    .ToHashSet(
                        StringComparer.Ordinal);

            var notifications =
                new List<Notification>();

            AddHostApplicationNotifications(
                hostApplicationEntries,
                trackedNotificationKeys,
                notifications);

            AddPropertyNotifications(
                propertyEntries,
                trackedNotificationKeys,
                notifications);

            if (notifications.Count == 0)
            {
                return;
            }

            /*
             * These entities are inserted in the same
             * SaveChanges call as the moderation decision.
             */
            dbContext.Notifications.AddRange(
                notifications);
        }

        private static bool
            IsHostApplicationModerationTransition(
                EntityEntry<HostProfile> entry)
        {
            if (entry.State !=
                EntityState.Modified)
            {
                return false;
            }

            var statusProperty =
                entry.Property(
                    hostProfile =>
                        hostProfile.Status);

            if (!statusProperty.IsModified
                ||
                statusProperty.OriginalValue ==
                    statusProperty.CurrentValue)
            {
                return false;
            }

            return statusProperty.CurrentValue is
                HostApplicationStatus.Approved
                or
                HostApplicationStatus.Rejected;
        }

        private static bool
            IsPropertyModerationTransition(
                EntityEntry<Property> entry)
        {
            if (entry.State !=
                EntityState.Modified)
            {
                return false;
            }

            var statusProperty =
                entry.Property(
                    property =>
                        property.Status);

            if (!statusProperty.IsModified
                ||
                statusProperty.OriginalValue ==
                    statusProperty.CurrentValue)
            {
                return false;
            }

            return statusProperty.CurrentValue is
                PropertyStatus.Published
                or
                PropertyStatus.Rejected;
        }

        private static void
            AddHostApplicationNotifications(
                IReadOnlyCollection<
                    EntityEntry<HostProfile>>
                    entries,
                ISet<string>
                    trackedNotificationKeys,
                ICollection<Notification>
                    notifications)
        {
            foreach (var entry in entries)
            {
                var hostProfile =
                    entry.Entity;

                ValidateIdentifier(
                    hostProfile.Id,
                    "host application");

                ValidateIdentifier(
                    hostProfile.UserId,
                    "host application user");

                /*
                 * AdminHostApplicationService always sets
                 * ReviewedAt before saving an approval or
                 * rejection.
                 *
                 * Requiring it here produces stable and unique
                 * event-level deduplication keys.
                 */
                var reviewedAt =
                    hostProfile.ReviewedAt
                    ??
                    throw new InvalidOperationException(
                        "A moderated host application must contain a review timestamp.");

                Notification notification;

                switch (hostProfile.Status)
                {
                    case HostApplicationStatus.Approved:
                        notification =
                            new Notification
                            {
                                Id =
                                    Guid.NewGuid(),

                                UserId =
                                    hostProfile.UserId,

                                Type =
                                    NotificationType
                                        .HostApplicationApproved,

                                Title =
                                    "Host application approved",

                                Message =
                                    "Your host application was approved. " +
                                    "You can now create and manage " +
                                    "properties on SmartStay.",

                                ReferenceType =
                                    NotificationReferenceType
                                        .HostApplication,

                                ReferenceId =
                                    hostProfile.Id,

                                DeduplicationKey =
                                    NotificationDeduplicationKeys
                                        .HostApplicationApproved(
                                            hostProfile.Id,
                                            reviewedAt),

                                CreatedAt =
                                    reviewedAt,

                                ReadAt =
                                    null
                            };
                        break;

                    case HostApplicationStatus.Rejected:
                        var rejectionReason =
                            NormalizeReason(
                                hostProfile
                                    .RejectionReason);

                        notification =
                            new Notification
                            {
                                Id =
                                    Guid.NewGuid(),

                                UserId =
                                    hostProfile.UserId,

                                Type =
                                    NotificationType
                                        .HostApplicationRejected,

                                Title =
                                    "Host application needs changes",

                                Message =
                                    "Your host application was rejected. " +
                                    $"Reason: {rejectionReason}",

                                ReferenceType =
                                    NotificationReferenceType
                                        .HostApplication,

                                ReferenceId =
                                    hostProfile.Id,

                                DeduplicationKey =
                                    NotificationDeduplicationKeys
                                        .HostApplicationRejected(
                                            hostProfile.Id,
                                            reviewedAt),

                                CreatedAt =
                                    reviewedAt,

                                ReadAt =
                                    null
                            };
                        break;

                    default:
                        continue;
                }

                AddCandidate(
                    notification,
                    trackedNotificationKeys,
                    notifications);
            }
        }

        private static void AddPropertyNotifications(
            IReadOnlyCollection<
                EntityEntry<Property>>
                entries,
            ISet<string>
                trackedNotificationKeys,
            ICollection<Notification>
                notifications)
        {
            foreach (var entry in entries)
            {
                var property =
                    entry.Entity;

                ValidateIdentifier(
                    property.Id,
                    "property");

                /*
                 * AdminPropertyService loads HostProfile and
                 * its User before changing the moderation
                 * status.
                 */
                var hostProfile =
                    entry.Reference(
                            item =>
                                item.HostProfile)
                        .CurrentValue;

                if (hostProfile is null)
                {
                    throw new InvalidOperationException(
                        "The property's host profile must be loaded before saving a moderation decision.");
                }

                ValidateIdentifier(
                    hostProfile.UserId,
                    "property host user");

                var reviewedAt =
                    property.ReviewedAt
                    ??
                    throw new InvalidOperationException(
                        "A moderated property must contain a review timestamp.");

                var propertyTitle =
                    NormalizePropertyTitle(
                        property.Title);

                Notification notification;

                switch (property.Status)
                {
                    case PropertyStatus.Published:
                        notification =
                            new Notification
                            {
                                Id =
                                    Guid.NewGuid(),

                                UserId =
                                    hostProfile.UserId,

                                Type =
                                    NotificationType
                                        .PropertyPublished,

                                Title =
                                    "Property published",

                                Message =
                                    $"Your property \"{propertyTitle}\" " +
                                    "was approved and is now live " +
                                    "on SmartStay.",

                                ReferenceType =
                                    NotificationReferenceType
                                        .Property,

                                ReferenceId =
                                    property.Id,

                                DeduplicationKey =
                                    NotificationDeduplicationKeys
                                        .PropertyPublished(
                                            property.Id,
                                            reviewedAt),

                                CreatedAt =
                                    reviewedAt,

                                ReadAt =
                                    null
                            };
                        break;

                    case PropertyStatus.Rejected:
                        var rejectionReason =
                            NormalizeReason(
                                property
                                    .RejectionReason);

                        notification =
                            new Notification
                            {
                                Id =
                                    Guid.NewGuid(),

                                UserId =
                                    hostProfile.UserId,

                                Type =
                                    NotificationType
                                        .PropertyRejected,

                                Title =
                                    "Property needs changes",

                                Message =
                                    $"Your property \"{propertyTitle}\" " +
                                    "was rejected. " +
                                    $"Reason: {rejectionReason}",

                                ReferenceType =
                                    NotificationReferenceType
                                        .Property,

                                ReferenceId =
                                    property.Id,

                                DeduplicationKey =
                                    NotificationDeduplicationKeys
                                        .PropertyRejected(
                                            property.Id,
                                            reviewedAt),

                                CreatedAt =
                                    reviewedAt,

                                ReadAt =
                                    null
                            };
                        break;

                    default:
                        continue;
                }

                AddCandidate(
                    notification,
                    trackedNotificationKeys,
                    notifications);
            }
        }

        private static void AddCandidate(
            Notification notification,
            ISet<string> trackedNotificationKeys,
            ICollection<Notification> notifications)
        {
            var deduplicationKey =
                notification.DeduplicationKey
                ??
                throw new InvalidOperationException(
                    "A moderation notification must contain a deduplication key.");

            var trackedKey =
                BuildTrackedKey(
                    notification.UserId,
                    deduplicationKey);

            if (!trackedNotificationKeys.Add(
                    trackedKey))
            {
                return;
            }

            notifications.Add(
                notification);
        }

        private static string BuildTrackedKey(
            Guid userId,
            string deduplicationKey)
        {
            return
                $"{userId:N}|" +
                deduplicationKey
                    .Trim()
                    .ToLowerInvariant();
        }

        private static string NormalizeReason(
            string? reason)
        {
            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                return
                    "Please review the submitted information and try again.";
            }

            return reason.Trim();
        }

        private static string NormalizePropertyTitle(
            string? title)
        {
            if (string.IsNullOrWhiteSpace(
                    title))
            {
                return "your property";
            }

            return title.Trim();
        }

        private static void ValidateIdentifier(
            Guid identifier,
            string identifierName)
        {
            if (identifier == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"The {identifierName} identifier is invalid.");
            }
        }
    }
}