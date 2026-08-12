using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class BookingPaymentNotificationInterceptor
        : SaveChangesInterceptor
    {
        private const int SqlParameterBatchSize =
            500;

        public override async ValueTask<
            InterceptionResult<int>>
            SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
        {
            if (eventData.Context
                is not SmartStayDbContext dbContext)
            {
                return await base.SavingChangesAsync(
                    eventData,
                    result,
                    cancellationToken);
            }

            dbContext.ChangeTracker.DetectChanges();

            var bookingChanges =
                dbContext.ChangeTracker
                    .Entries<Booking>()
                    .Where(
                        IsRelevantBookingEntry)
                    .ToList();

            var paymentChanges =
                dbContext.ChangeTracker
                    .Entries<BookingPayment>()
                    .Where(
                        IsRelevantPaymentEntry)
                    .ToList();

            if (bookingChanges.Count == 0
                &&
                paymentChanges.Count == 0)
            {
                return await base.SavingChangesAsync(
                    eventData,
                    result,
                    cancellationToken);
            }

            var bookingContexts =
                await BuildBookingContextsAsync(
                    dbContext,
                    bookingChanges,
                    paymentChanges,
                    cancellationToken);

            var propertyIds =
                bookingContexts.Values
                    .Select(item =>
                        item.PropertyId)
                    .Where(propertyId =>
                        propertyId != Guid.Empty)
                    .Distinct()
                    .ToArray();

            var properties =
                await LoadPropertyNotificationDataAsync(
                    dbContext,
                    propertyIds,
                    cancellationToken);

            var candidates =
                new List<NotificationCandidate>();

            AddBookingNotifications(
                bookingChanges,
                bookingContexts,
                properties,
                candidates);

            AddPaymentNotifications(
                paymentChanges,
                bookingContexts,
                properties,
                candidates);

            AddNotificationsToContext(
                dbContext,
                candidates);

            return await base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        private static bool IsRelevantBookingEntry(
            EntityEntry<Booking> entry)
        {
            if (entry.State == EntityState.Added)
            {
                return entry.Entity.Status ==
                    BookingStatus.Pending;
            }

            return entry.State ==
                       EntityState.Modified
                   &&
                   entry.Property(
                           booking =>
                               booking.Status)
                       .IsModified
                   &&
                   entry.Property(
                           booking =>
                               booking.Status)
                       .OriginalValue
                   !=
                   entry.Property(
                           booking =>
                               booking.Status)
                       .CurrentValue;
        }

        private static bool IsRelevantPaymentEntry(
            EntityEntry<BookingPayment> entry)
        {
            if (entry.State !=
                EntityState.Modified)
            {
                return false;
            }

            var statusProperty =
                entry.Property(
                    payment =>
                        payment.Status);

            return statusProperty.IsModified
                   &&
                   statusProperty.OriginalValue
                   !=
                   statusProperty.CurrentValue
                   &&
                   statusProperty.CurrentValue is
                       PaymentStatus.Succeeded
                       or PaymentStatus.Failed
                       or PaymentStatus.Cancelled
                       or PaymentStatus.PartiallyRefunded
                       or PaymentStatus.Refunded;
        }

        private static async Task<
            Dictionary<
                Guid,
                BookingNotificationContext>>
            BuildBookingContextsAsync(
                SmartStayDbContext dbContext,
                IReadOnlyList<
                    EntityEntry<Booking>>
                    bookingChanges,
                IReadOnlyList<
                    EntityEntry<BookingPayment>>
                    paymentChanges,
                CancellationToken cancellationToken)
        {
            var result =
                bookingChanges
                    .Select(entry =>
                        entry.Entity)
                    .Where(booking =>
                        booking.Id != Guid.Empty)
                    .GroupBy(booking =>
                        booking.Id)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                        {
                            var booking =
                                group.First();

                            return new
                                BookingNotificationContext
                            {
                                BookingId =
                                        booking.Id,

                                GuestUserId =
                                        booking
                                            .GuestUserId,

                                PropertyId =
                                        booking.PropertyId,

                                ExpiresAt =
                                        booking.ExpiresAt
                            };
                        });

            foreach (var paymentEntry
                     in paymentChanges)
            {
                var payment =
                    paymentEntry.Entity;

                if (result.ContainsKey(
                        payment.BookingId))
                {
                    continue;
                }

                if (payment.Booking is not null
                    &&
                    payment.Booking.Id !=
                        Guid.Empty)
                {
                    result[payment.BookingId] =
                        new BookingNotificationContext
                        {
                            BookingId =
                                payment.Booking.Id,

                            GuestUserId =
                                payment.Booking
                                    .GuestUserId,

                            PropertyId =
                                payment.Booking
                                    .PropertyId,

                            ExpiresAt =
                                payment.Booking
                                    .ExpiresAt
                        };
                }
            }

            var missingBookingIds =
                paymentChanges
                    .Select(entry =>
                        entry.Entity.BookingId)
                    .Where(bookingId =>
                        bookingId != Guid.Empty
                        &&
                        !result.ContainsKey(
                            bookingId))
                    .Distinct()
                    .ToArray();

            if (missingBookingIds.Length == 0)
            {
                return result;
            }

            var loadedContexts =
                await LoadBookingNotificationContextsAsync(
                    dbContext,
                    missingBookingIds,
                    cancellationToken);

            foreach (var item
                     in loadedContexts)
            {
                result[item.Key] =
                    item.Value;
            }

            return result;
        }

        private static void AddBookingNotifications(
            IReadOnlyList<
                EntityEntry<Booking>>
                bookingChanges,
            IReadOnlyDictionary<
                Guid,
                BookingNotificationContext>
                bookingContexts,
            IReadOnlyDictionary<
                Guid,
                PropertyNotificationData>
                properties,
            ICollection<NotificationCandidate>
                candidates)
        {
            foreach (var entry
                     in bookingChanges)
            {
                var booking =
                    entry.Entity;

                if (!bookingContexts.TryGetValue(
                        booking.Id,
                        out var bookingContext))
                {
                    continue;
                }

                if (!properties.TryGetValue(
                        bookingContext.PropertyId,
                        out var property))
                {
                    continue;
                }

                if (entry.State ==
                    EntityState.Added)
                {
                    AddBookingPendingPaymentNotification(
                        bookingContext,
                        property,
                        candidates);

                    continue;
                }

                var currentStatus =
                    entry.Property(
                            item =>
                                item.Status)
                        .CurrentValue;

                switch (currentStatus)
                {
                    case BookingStatus.Confirmed:
                        AddBookingConfirmedNotifications(
                            bookingContext,
                            property,
                            candidates);
                        break;

                    case BookingStatus.Cancelled:
                        AddBookingCancelledNotifications(
                            bookingContext,
                            property,
                            candidates);
                        break;

                    case BookingStatus.Expired:
                        AddBookingExpiredNotification(
                            bookingContext,
                            property,
                            candidates);
                        break;

                    case BookingStatus.Completed:
                        AddBookingCompletedNotifications(
                            bookingContext,
                            property,
                            candidates);
                        break;
                }
            }
        }

        private static void AddPaymentNotifications(
            IReadOnlyList<
                EntityEntry<BookingPayment>>
                paymentChanges,
            IReadOnlyDictionary<
                Guid,
                BookingNotificationContext>
                bookingContexts,
            IReadOnlyDictionary<
                Guid,
                PropertyNotificationData>
                properties,
            ICollection<NotificationCandidate>
                candidates)
        {
            foreach (var entry
                     in paymentChanges)
            {
                var payment =
                    entry.Entity;

                if (!bookingContexts.TryGetValue(
                        payment.BookingId,
                        out var bookingContext))
                {
                    continue;
                }

                if (!properties.TryGetValue(
                        bookingContext.PropertyId,
                        out var property))
                {
                    continue;
                }

                var status =
                    entry.Property(
                            item =>
                                item.Status)
                        .CurrentValue;

                var currency =
                    string.IsNullOrWhiteSpace(
                        payment.Currency)
                        ? string.Empty
                        : payment.Currency
                            .Trim()
                            .ToUpperInvariant();

                switch (status)
                {
                    case PaymentStatus.Succeeded:
                        candidates.Add(
                            new NotificationCandidate
                            {
                                UserId =
                                    bookingContext
                                        .GuestUserId,

                                Type =
                                    NotificationType
                                        .PaymentSucceeded,

                                Title =
                                    "Payment successful",

                                Message =
                                    $"Your payment of " +
                                    $"{payment.Amount:0.00} " +
                                    $"{currency} for " +
                                    $"\"{property.Title}\" " +
                                    $"was completed successfully.",

                                ReferenceType =
                                    NotificationReferenceType
                                        .Payment,

                                ReferenceId =
                                    payment.Id,

                                DeduplicationKey =
                                    NotificationDeduplicationKeys
                                        .PaymentSucceeded(
                                            payment.Id)
                            });
                        break;

                    case PaymentStatus.Failed:
                    case PaymentStatus.Cancelled:
                        candidates.Add(
                            new NotificationCandidate
                            {
                                UserId =
                                    bookingContext
                                        .GuestUserId,

                                Type =
                                    NotificationType
                                        .PaymentFailed,

                                Title =
                                    status ==
                                    PaymentStatus.Cancelled
                                        ? "Payment cancelled"
                                        : "Payment failed",

                                Message =
                                    status ==
                                    PaymentStatus.Cancelled
                                        ? $"Your payment for " +
                                          $"\"{property.Title}\" " +
                                          $"was cancelled. You can " +
                                          $"start a new payment attempt " +
                                          $"while the booking is active."
                                        : $"Your payment for " +
                                          $"\"{property.Title}\" " +
                                          $"was not completed. Please " +
                                          $"try again while the booking " +
                                          $"is active.",

                                ReferenceType =
                                    NotificationReferenceType
                                        .Payment,

                                ReferenceId =
                                    payment.Id,

                                DeduplicationKey =
                                    status ==
                                    PaymentStatus.Cancelled
                                        ? NotificationDeduplicationKeys
                                            .PaymentCancelled(
                                                payment.Id)
                                        : NotificationDeduplicationKeys
                                            .PaymentFailed(
                                                payment.Id)
                            });
                        break;

                    case PaymentStatus.PartiallyRefunded:
                    case PaymentStatus.Refunded:
                        candidates.Add(
                            new NotificationCandidate
                            {
                                UserId =
                                    bookingContext
                                        .GuestUserId,

                                Type =
                                    NotificationType
                                        .PaymentRefunded,

                                Title =
                                    status ==
                                    PaymentStatus.Refunded
                                        ? "Payment refunded"
                                        : "Partial refund processed",

                                Message =
                                    status ==
                                    PaymentStatus.Refunded
                                        ? $"Your payment for " +
                                          $"\"{property.Title}\" " +
                                          $"was refunded."
                                        : $"A partial refund for " +
                                          $"\"{property.Title}\" " +
                                          $"was processed.",

                                ReferenceType =
                                    NotificationReferenceType
                                        .Payment,

                                ReferenceId =
                                    payment.Id,

                                DeduplicationKey =
                                    status ==
                                    PaymentStatus
                                        .PartiallyRefunded
                                        ? NotificationDeduplicationKeys
                                            .PaymentPartiallyRefunded(
                                                payment.Id)
                                        : NotificationDeduplicationKeys
                                            .PaymentRefunded(
                                                payment.Id)
                            });
                        break;
                }
            }
        }

        private static void
            AddBookingPendingPaymentNotification(
                BookingNotificationContext booking,
                PropertyNotificationData property,
                ICollection<NotificationCandidate>
                    candidates)
        {
            var deadline =
                booking.ExpiresAt.HasValue
                    ? $" Complete payment before " +
                      $"{booking.ExpiresAt.Value:O}."
                    : string.Empty;

            candidates.Add(
                new NotificationCandidate
                {
                    UserId =
                        booking.GuestUserId,

                    Type =
                        NotificationType
                            .BookingPendingPayment,

                    Title =
                        "Complete your payment",

                    Message =
                        $"Your booking for " +
                        $"\"{property.Title}\" " +
                        $"is waiting for payment." +
                        deadline,

                    ReferenceType =
                        NotificationReferenceType
                            .Booking,

                    ReferenceId =
                        booking.BookingId,

                    DeduplicationKey =
                        NotificationDeduplicationKeys
                            .BookingPendingPayment(
                                booking.BookingId)
                });
        }

        private static void
            AddBookingConfirmedNotifications(
                BookingNotificationContext booking,
                PropertyNotificationData property,
                ICollection<NotificationCandidate>
                    candidates)
        {
            candidates.Add(
                new NotificationCandidate
                {
                    UserId =
                        booking.GuestUserId,

                    Type =
                        NotificationType
                            .BookingConfirmed,

                    Title =
                        "Booking confirmed",

                    Message =
                        $"Your booking for " +
                        $"\"{property.Title}\" " +
                        $"has been confirmed.",

                    ReferenceType =
                        NotificationReferenceType
                            .Booking,

                    ReferenceId =
                        booking.BookingId,

                    DeduplicationKey =
                        NotificationDeduplicationKeys
                            .BookingConfirmed(
                                booking.BookingId)
                });

            candidates.Add(
                new NotificationCandidate
                {
                    UserId =
                        property.HostUserId,

                    Type =
                        NotificationType
                            .NewBookingReceived,

                    Title =
                        "New confirmed booking",

                    Message =
                        $"You received a new confirmed " +
                        $"booking for " +
                        $"\"{property.Title}\".",

                    ReferenceType =
                        NotificationReferenceType
                            .Booking,

                    ReferenceId =
                        booking.BookingId,

                    DeduplicationKey =
                        NotificationDeduplicationKeys
                            .NewBookingReceived(
                                booking.BookingId)
                });
        }

        private static void
            AddBookingCancelledNotifications(
                BookingNotificationContext booking,
                PropertyNotificationData property,
                ICollection<NotificationCandidate>
                    candidates)
        {
            var deduplicationKey =
                NotificationDeduplicationKeys
                    .BookingCancelled(
                        booking.BookingId);

            candidates.Add(
                new NotificationCandidate
                {
                    UserId =
                        booking.GuestUserId,

                    Type =
                        NotificationType
                            .BookingCancelled,

                    Title =
                        "Booking cancelled",

                    Message =
                        $"Your booking for " +
                        $"\"{property.Title}\" " +
                        $"was cancelled.",

                    ReferenceType =
                        NotificationReferenceType
                            .Booking,

                    ReferenceId =
                        booking.BookingId,

                    DeduplicationKey =
                        deduplicationKey
                });

            candidates.Add(
                new NotificationCandidate
                {
                    UserId =
                        property.HostUserId,

                    Type =
                        NotificationType
                            .BookingCancelled,

                    Title =
                        "Reservation cancelled",

                    Message =
                        $"The reservation for " +
                        $"\"{property.Title}\" " +
                        $"was cancelled by the guest.",

                    ReferenceType =
                        NotificationReferenceType
                            .Booking,

                    ReferenceId =
                        booking.BookingId,

                    DeduplicationKey =
                        deduplicationKey
                });
        }

        private static void
            AddBookingExpiredNotification(
                BookingNotificationContext booking,
                PropertyNotificationData property,
                ICollection<NotificationCandidate>
                    candidates)
        {
            candidates.Add(
                new NotificationCandidate
                {
                    UserId =
                        booking.GuestUserId,

                    Type =
                        NotificationType
                            .BookingExpired,

                    Title =
                        "Booking expired",

                    Message =
                        $"Your booking for " +
                        $"\"{property.Title}\" expired " +
                        $"because payment was not completed " +
                        $"within the allowed time.",

                    ReferenceType =
                        NotificationReferenceType
                            .Booking,

                    ReferenceId =
                        booking.BookingId,

                    DeduplicationKey =
                        NotificationDeduplicationKeys
                            .BookingExpired(
                                booking.BookingId)
                });
        }

        private static void
            AddBookingCompletedNotifications(
                BookingNotificationContext booking,
                PropertyNotificationData property,
                ICollection<NotificationCandidate>
                    candidates)
        {
            var deduplicationKey =
                NotificationDeduplicationKeys
                    .BookingCompleted(
                        booking.BookingId);

            candidates.Add(
                new NotificationCandidate
                {
                    UserId =
                        booking.GuestUserId,

                    Type =
                        NotificationType
                            .BookingCompleted,

                    Title =
                        "Stay completed",

                    Message =
                        $"Your stay at " +
                        $"\"{property.Title}\" is complete. " +
                        $"You can now share your review.",

                    ReferenceType =
                        NotificationReferenceType
                            .Booking,

                    ReferenceId =
                        booking.BookingId,

                    DeduplicationKey =
                        deduplicationKey
                });

            candidates.Add(
                new NotificationCandidate
                {
                    UserId =
                        property.HostUserId,

                    Type =
                        NotificationType
                            .BookingCompleted,

                    Title =
                        "Reservation completed",

                    Message =
                        $"The reservation for " +
                        $"\"{property.Title}\" " +
                        $"has been completed.",

                    ReferenceType =
                        NotificationReferenceType
                            .Booking,

                    ReferenceId =
                        booking.BookingId,

                    DeduplicationKey =
                        deduplicationKey
                });
        }

        private static void AddNotificationsToContext(
            SmartStayDbContext dbContext,
            IEnumerable<NotificationCandidate>
                candidates)
        {
            var trackedKeys =
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

            var uniqueCandidates =
                candidates
                    .Where(candidate =>
                        candidate.UserId !=
                            Guid.Empty)
                    .GroupBy(
                        candidate =>
                            BuildTrackedKey(
                                candidate.UserId,
                                candidate
                                    .DeduplicationKey),
                        StringComparer.Ordinal)
                    .Select(group =>
                        group.First());

            foreach (var candidate
                     in uniqueCandidates)
            {
                var trackedKey =
                    BuildTrackedKey(
                        candidate.UserId,
                        candidate
                            .DeduplicationKey);

                if (!trackedKeys.Add(
                        trackedKey))
                {
                    continue;
                }

                dbContext.Notifications.Add(
                    new Notification
                    {
                        Id =
                            Guid.NewGuid(),

                        UserId =
                            candidate.UserId,

                        Type =
                            candidate.Type,

                        Title =
                            candidate.Title,

                        Message =
                            candidate.Message,

                        ReferenceType =
                            candidate
                                .ReferenceType,

                        ReferenceId =
                            candidate.ReferenceId,

                        DeduplicationKey =
                            candidate
                                .DeduplicationKey,

                        CreatedAt =
                            DateTimeOffset.UtcNow,

                        ReadAt =
                            null
                    });
            }
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

        private static async Task<
            Dictionary<
                Guid,
                BookingNotificationContext>>
            LoadBookingNotificationContextsAsync(
                SmartStayDbContext dbContext,
                IReadOnlyCollection<Guid>
                    bookingIds,
                CancellationToken cancellationToken)
        {
            var result =
                new Dictionary<
                    Guid,
                    BookingNotificationContext>();

            if (bookingIds.Count == 0)
            {
                return result;
            }

            var connection =
                dbContext.Database
                    .GetDbConnection();

            var shouldClose =
                connection.State !=
                ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            try
            {
                foreach (var batch
                         in bookingIds.Chunk(
                             SqlParameterBatchSize))
                {
                    await using var command =
                        connection.CreateCommand();

                    AttachCurrentTransaction(
                        dbContext,
                        command);

                    var parameterNames =
                        AddGuidParameters(
                            command,
                            batch,
                            "bookingId");

                    command.CommandText =
                        $"""
                        SELECT
                            [Id],
                            [GuestUserId],
                            [PropertyId],
                            [ExpiresAt]
                        FROM [Bookings]
                        WHERE [Id] IN
                        ({string.Join(
                            ", ",
                            parameterNames)});
                        """;

                    await using var reader =
                        await command
                            .ExecuteReaderAsync(
                                cancellationToken);

                    while (await reader.ReadAsync(
                               cancellationToken))
                    {
                        var bookingId =
                            reader.GetGuid(0);

                        result[bookingId] =
                            new
                                BookingNotificationContext
                            {
                                BookingId =
                                        bookingId,

                                GuestUserId =
                                        reader.GetGuid(1),

                                PropertyId =
                                        reader.GetGuid(2),

                                ExpiresAt =
                                        reader.IsDBNull(3)
                                            ? null
                                            : reader
                                                .GetFieldValue<
                                                    DateTimeOffset>(
                                                    3)
                            };
                    }
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }

        private static async Task<
            Dictionary<
                Guid,
                PropertyNotificationData>>
            LoadPropertyNotificationDataAsync(
                SmartStayDbContext dbContext,
                IReadOnlyCollection<Guid>
                    propertyIds,
                CancellationToken cancellationToken)
        {
            var result =
                new Dictionary<
                    Guid,
                    PropertyNotificationData>();

            if (propertyIds.Count == 0)
            {
                return result;
            }

            var connection =
                dbContext.Database
                    .GetDbConnection();

            var shouldClose =
                connection.State !=
                ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            try
            {
                foreach (var batch
                         in propertyIds.Chunk(
                             SqlParameterBatchSize))
                {
                    await using var command =
                        connection.CreateCommand();

                    AttachCurrentTransaction(
                        dbContext,
                        command);

                    var parameterNames =
                        AddGuidParameters(
                            command,
                            batch,
                            "propertyId");

                    command.CommandText =
                        $"""
                        SELECT
                            [property].[Id],
                            [property].[Title],
                            [host].[UserId]
                        FROM [Properties]
                            AS [property]
                        INNER JOIN [HostProfiles]
                            AS [host]
                            ON [host].[Id] =
                               [property].[HostProfileId]
                        WHERE [property].[Id] IN
                        ({string.Join(
                            ", ",
                            parameterNames)});
                        """;

                    await using var reader =
                        await command
                            .ExecuteReaderAsync(
                                cancellationToken);

                    while (await reader.ReadAsync(
                               cancellationToken))
                    {
                        var propertyId =
                            reader.GetGuid(0);

                        result[propertyId] =
                            new
                                PropertyNotificationData
                            {
                                PropertyId =
                                        propertyId,

                                Title =
                                        reader.GetString(1),

                                HostUserId =
                                        reader.GetGuid(2)
                            };
                    }
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }

        private static IReadOnlyList<string>
            AddGuidParameters(
                DbCommand command,
                IEnumerable<Guid> values,
                string parameterPrefix)
        {
            var names =
                new List<string>();

            var index =
                0;

            foreach (var value in values)
            {
                var parameter =
                    command.CreateParameter();

                parameter.ParameterName =
                    $"@{parameterPrefix}{index}";

                parameter.DbType =
                    DbType.Guid;

                parameter.Value =
                    value;

                command.Parameters.Add(
                    parameter);

                names.Add(
                    parameter.ParameterName);

                index++;
            }

            return names;
        }

        private static void AttachCurrentTransaction(
            SmartStayDbContext dbContext,
            DbCommand command)
        {
            var currentTransaction =
                dbContext.Database
                    .CurrentTransaction;

            if (currentTransaction is not null)
            {
                command.Transaction =
                    currentTransaction
                        .GetDbTransaction();
            }
        }

        private sealed class
            BookingNotificationContext
        {
            public Guid BookingId { get; set; }

            public Guid GuestUserId { get; set; }

            public Guid PropertyId { get; set; }

            public DateTimeOffset? ExpiresAt
            { get; set; }
        }

        private sealed class
            PropertyNotificationData
        {
            public Guid PropertyId { get; set; }

            public Guid HostUserId { get; set; }

            public string Title { get; set; } =
                string.Empty;
        }

        private sealed class NotificationCandidate
        {
            public Guid UserId { get; set; }

            public NotificationType Type { get; set; }

            public string Title { get; set; } =
                string.Empty;

            public string Message { get; set; } =
                string.Empty;

            public NotificationReferenceType
                ReferenceType
            { get; set; }

            public Guid ReferenceId { get; set; }

            public string DeduplicationKey
            { get; set; } =
                string.Empty;
        }
    }
}