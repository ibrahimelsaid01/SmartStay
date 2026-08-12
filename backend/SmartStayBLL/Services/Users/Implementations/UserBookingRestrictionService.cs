using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class UserBookingRestrictionService
        : IUserBookingRestrictionService
    {
        private const int CancellationLookbackDays =
            90;

        private const int WarningConfirmedCancellationThreshold =
            3;

        private const int AdminReviewConfirmedCancellationThreshold =
            4;

        private const int MaximumReasonLength =
            1000;

        private readonly SmartStayDbContext
            _dbContext;

        public UserBookingRestrictionService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        public async Task EnsureUserCanCreateBookingAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

            await ExpireOutdatedRestrictionsAsync(
                userId,
                cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            var activeRestriction =
                await _dbContext.UserBookingRestrictions
                    .AsNoTracking()
                    .Where(
                        restriction =>
                            restriction.UserId == userId
                            &&
                            restriction.Status ==
                                UserBookingRestrictionStatus.Active
                            &&
                            restriction.Type ==
                                UserBookingRestrictionType
                                    .TemporaryBookingRestriction
                            &&
                            restriction.RestrictedFrom <=
                                currentTime
                            &&
                            (
                                restriction.RestrictedUntil == null
                                ||
                                restriction.RestrictedUntil >
                                    currentTime
                            ))
                    .OrderByDescending(
                        restriction =>
                            restriction.CreatedAt)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (activeRestriction is null)
            {
                return;
            }

            var untilText =
                activeRestriction.RestrictedUntil.HasValue
                    ? activeRestriction.RestrictedUntil.Value.ToString("O")
                    : "further admin review";

            throw new InvalidOperationException(
                "Your account is temporarily restricted from creating new bookings " +
                $"until {untilText}. Reason: {activeRestriction.Reason}");
        }

        public async Task<UserBookingRestrictionResponse?> GetActiveBookingRestrictionAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

            await ExpireOutdatedRestrictionsAsync(
                userId,
                cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            var restriction =
                await _dbContext.UserBookingRestrictions
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.CreatedByAdmin)
                    .Include(
                        item =>
                            item.RemovedByAdmin)
                    .Where(
                        item =>
                            item.UserId == userId
                            &&
                            item.Status ==
                                UserBookingRestrictionStatus.Active
                            &&
                            item.Type ==
                                UserBookingRestrictionType
                                    .TemporaryBookingRestriction
                            &&
                            item.RestrictedFrom <=
                                currentTime
                            &&
                            (
                                item.RestrictedUntil == null
                                ||
                                item.RestrictedUntil >
                                    currentTime
                            ))
                    .OrderByDescending(
                        item =>
                            item.CreatedAt)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            return restriction is null
                ? null
                : MapToResponse(
                    restriction);
        }

        public async Task<UserBookingRestrictionResponse?> EvaluateGuestCancellationAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

            await ExpireOutdatedRestrictionsAsync(
                userId,
                cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            var lookbackStart =
                currentTime.AddDays(
                    -CancellationLookbackDays);

            var cancellationCount =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .CountAsync(
                        booking =>
                            booking.GuestUserId == userId
                            &&
                            booking.Status ==
                                BookingStatus.Cancelled
                            &&
                            booking.ConfirmedAt.HasValue
                            &&
                            booking.CancelledAt.HasValue
                            &&
                            booking.CancelledAt.Value >=
                                lookbackStart,
                        cancellationToken);

            if (cancellationCount <
                WarningConfirmedCancellationThreshold)
            {
                return null;
            }

            if (cancellationCount >=
                AdminReviewConfirmedCancellationThreshold)
            {
                var existingAdminReviewFlag =
                    await GetActiveRestrictionEntityAsync(
                        userId,
                        UserBookingRestrictionType
                            .AdminReviewFlag,
                        cancellationToken);

                if (existingAdminReviewFlag is not null)
                {
                    return MapToResponse(
                        existingAdminReviewFlag);
                }

                var reason =
                    NormalizeReason(
                        $"The guest cancelled {cancellationCount} confirmed bookings " +
                        $"within the last {CancellationLookbackDays} days. " +
                        "The account was flagged for admin review. " +
                        "No booking suspension was applied automatically.");

                var adminReviewFlag =
                    new UserBookingRestriction
                    {
                        Id =
                            Guid.NewGuid(),

                        UserId =
                            userId,

                        Type =
                            UserBookingRestrictionType
                                .AdminReviewFlag,

                        Status =
                            UserBookingRestrictionStatus.Active,

                        Reason =
                            reason,

                        CancellationCountSnapshot =
                            cancellationCount,

                        RestrictedFrom =
                            currentTime,

                        RestrictedUntil =
                            null,

                        CreatedBySystem =
                            true,

                        CreatedByAdminId =
                            null,

                        CreatedAt =
                            currentTime,

                        UpdatedAt =
                            null,

                        RemovedByAdminId =
                            null,

                        RemovedAt =
                            null,

                        RemovalNote =
                            null
                    };

                await _dbContext.UserBookingRestrictions
                    .AddAsync(
                        adminReviewFlag,
                        cancellationToken);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                return MapToResponse(
                    adminReviewFlag);
            }

            var existingWarning =
                await GetActiveRestrictionEntityAsync(
                    userId,
                    UserBookingRestrictionType.Warning,
                    cancellationToken);

            if (existingWarning is not null)
            {
                return MapToResponse(
                    existingWarning);
            }

            var warningReason =
                NormalizeReason(
                    $"The guest cancelled {cancellationCount} confirmed bookings " +
                    $"within the last {CancellationLookbackDays} days. " +
                    "A warning was created because repeated confirmed cancellations " +
                    "may lead to an admin review and a temporary booking suspension.");

            var warning =
                new UserBookingRestriction
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        userId,

                    Type =
                        UserBookingRestrictionType.Warning,

                    Status =
                        UserBookingRestrictionStatus.Active,

                    Reason =
                        warningReason,

                    CancellationCountSnapshot =
                        cancellationCount,

                    RestrictedFrom =
                        currentTime,

                    RestrictedUntil =
                        null,

                    CreatedBySystem =
                        true,

                    CreatedByAdminId =
                        null,

                    CreatedAt =
                        currentTime,

                    UpdatedAt =
                        null,

                    RemovedByAdminId =
                        null,

                    RemovedAt =
                        null,

                    RemovalNote =
                        null
                };

            await _dbContext.UserBookingRestrictions
                .AddAsync(
                    warning,
                    cancellationToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToResponse(
                warning);
        }

        public async Task<IReadOnlyList<UserBookingRestrictionResponse>> GetUserRestrictionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

            await ExpireOutdatedRestrictionsAsync(
                userId,
                cancellationToken);

            var restrictions =
                await _dbContext.UserBookingRestrictions
                    .AsNoTracking()
                    .Include(
                        restriction =>
                            restriction.CreatedByAdmin)
                    .Include(
                        restriction =>
                            restriction.RemovedByAdmin)
                    .Where(
                        restriction =>
                            restriction.UserId == userId)
                    .OrderByDescending(
                        restriction =>
                            restriction.CreatedAt)
                    .ToListAsync(
                        cancellationToken);

            return restrictions
                .Select(
                    restriction =>
                        MapToResponse(
                            restriction))
                .ToList();
        }

        public async Task<UserBookingRestrictionResponse> ApplyTemporaryBookingRestrictionAsync(
            Guid adminUserId,
            Guid adminReviewFlagId,
            ApplyTemporaryBookingRestrictionRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                adminUserId);

            ValidateRestrictionIdentifier(
                adminReviewFlagId);

            ArgumentNullException.ThrowIfNull(
                request);

            if (request.DurationDays < 1
                ||
                request.DurationDays > 90)
            {
                throw new ArgumentException(
                    "Temporary booking suspension duration must be between 1 and 90 days.");
            }

            var reason =
                NormalizeReason(
                    request.Reason);

            if (reason.Length < 10)
            {
                throw new ArgumentException(
                    "Temporary booking suspension reason must contain at least 10 characters.");
            }

            var adminReviewFlag =
                await _dbContext.UserBookingRestrictions
                    .SingleOrDefaultAsync(
                        restriction =>
                            restriction.Id == adminReviewFlagId,
                        cancellationToken);

            if (adminReviewFlag is null)
            {
                throw new KeyNotFoundException(
                    "The admin review flag was not found.");
            }

            if (adminReviewFlag.Type !=
                UserBookingRestrictionType.AdminReviewFlag)
            {
                throw new InvalidOperationException(
                    "Only an admin review flag can be converted into a temporary booking suspension.");
            }

            if (adminReviewFlag.Status !=
                UserBookingRestrictionStatus.Active)
            {
                throw new InvalidOperationException(
                    "The admin review flag is no longer active.");
            }

            await ExpireOutdatedRestrictionsAsync(
                adminReviewFlag.UserId,
                cancellationToken);

            var existingTemporaryRestriction =
                await GetActiveRestrictionEntityAsync(
                    adminReviewFlag.UserId,
                    UserBookingRestrictionType
                        .TemporaryBookingRestriction,
                    cancellationToken);

            if (existingTemporaryRestriction is not null)
            {
                throw new InvalidOperationException(
                    "The user already has an active temporary booking suspension.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            var restrictedUntil =
                currentTime.AddDays(
                    request.DurationDays);

            var temporaryRestriction =
                new UserBookingRestriction
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        adminReviewFlag.UserId,

                    Type =
                        UserBookingRestrictionType
                            .TemporaryBookingRestriction,

                    Status =
                        UserBookingRestrictionStatus.Active,

                    Reason =
                        reason,

                    CancellationCountSnapshot =
                        adminReviewFlag.CancellationCountSnapshot,

                    RestrictedFrom =
                        currentTime,

                    RestrictedUntil =
                        restrictedUntil,

                    CreatedBySystem =
                        false,

                    CreatedByAdminId =
                        adminUserId,

                    CreatedAt =
                        currentTime,

                    UpdatedAt =
                        null,

                    RemovedByAdminId =
                        null,

                    RemovedAt =
                        null,

                    RemovalNote =
                        null
                };

            adminReviewFlag.Status =
                UserBookingRestrictionStatus.Removed;

            adminReviewFlag.RemovedByAdminId =
                adminUserId;

            adminReviewFlag.RemovedAt =
                currentTime;

            adminReviewFlag.RemovalNote =
                $"Admin review completed. Temporary booking restriction " +
                $"{temporaryRestriction.Id} was applied until {restrictedUntil:O}.";

            adminReviewFlag.UpdatedAt =
                currentTime;

            await _dbContext.UserBookingRestrictions
                .AddAsync(
                    temporaryRestriction,
                    cancellationToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            var createdRestriction =
                await _dbContext.UserBookingRestrictions
                    .AsNoTracking()
                    .Include(
                        restriction =>
                            restriction.CreatedByAdmin)
                    .Include(
                        restriction =>
                            restriction.RemovedByAdmin)
                    .SingleAsync(
                        restriction =>
                            restriction.Id == temporaryRestriction.Id,
                        cancellationToken);

            return MapToResponse(
                createdRestriction);
        }

        public async Task<UserBookingRestrictionResponse> RemoveRestrictionAsync(
            Guid adminUserId,
            Guid restrictionId,
            RemoveUserBookingRestrictionRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                adminUserId);

            ValidateRestrictionIdentifier(
                restrictionId);

            ArgumentNullException.ThrowIfNull(
                request);

            var removalNote =
                NormalizeOptionalText(
                    request.RemovalNote,
                    MaximumReasonLength);

            var restriction =
                await _dbContext.UserBookingRestrictions
                    .Include(
                        item =>
                            item.CreatedByAdmin)
                    .Include(
                        item =>
                            item.RemovedByAdmin)
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == restrictionId,
                        cancellationToken);

            if (restriction is null)
            {
                throw new KeyNotFoundException(
                    "The user booking restriction was not found.");
            }

            if (restriction.Status ==
                UserBookingRestrictionStatus.Removed)
            {
                throw new InvalidOperationException(
                    "The user booking restriction has already been removed.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            restriction.Status =
                UserBookingRestrictionStatus.Removed;

            restriction.RemovedByAdminId =
                adminUserId;

            restriction.RemovedAt =
                currentTime;

            restriction.RemovalNote =
                removalNote;

            restriction.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            var updatedRestriction =
                await _dbContext.UserBookingRestrictions
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.CreatedByAdmin)
                    .Include(
                        item =>
                            item.RemovedByAdmin)
                    .SingleAsync(
                        item =>
                            item.Id == restriction.Id,
                        cancellationToken);

            return MapToResponse(
                updatedRestriction);
        }

        private async Task<UserBookingRestriction?> GetActiveRestrictionEntityAsync(
            Guid userId,
            UserBookingRestrictionType type,
            CancellationToken cancellationToken)
        {
            var currentTime =
                DateTimeOffset.UtcNow;

            return await _dbContext.UserBookingRestrictions
                .Include(
                    restriction =>
                        restriction.CreatedByAdmin)
                .Include(
                    restriction =>
                        restriction.RemovedByAdmin)
                .Where(
                    restriction =>
                        restriction.UserId == userId
                        &&
                        restriction.Type == type
                        &&
                        restriction.Status ==
                            UserBookingRestrictionStatus.Active
                        &&
                        restriction.RestrictedFrom <=
                            currentTime
                        &&
                        (
                            restriction.RestrictedUntil == null
                            ||
                            restriction.RestrictedUntil >
                                currentTime
                        ))
                .OrderByDescending(
                    restriction =>
                        restriction.CreatedAt)
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        private async Task ExpireOutdatedRestrictionsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var currentTime =
                DateTimeOffset.UtcNow;

            var expiredRestrictions =
                await _dbContext.UserBookingRestrictions
                    .Where(
                        restriction =>
                            restriction.UserId == userId
                            &&
                            restriction.Status ==
                                UserBookingRestrictionStatus.Active
                            &&
                            restriction.RestrictedUntil.HasValue
                            &&
                            restriction.RestrictedUntil.Value <=
                                currentTime)
                    .ToListAsync(
                        cancellationToken);

            if (expiredRestrictions.Count == 0)
            {
                return;
            }

            foreach (var restriction in expiredRestrictions)
            {
                restriction.Status =
                    UserBookingRestrictionStatus.Expired;

                restriction.UpdatedAt =
                    currentTime;
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        private static UserBookingRestrictionResponse MapToResponse(
            UserBookingRestriction restriction)
        {
            return new UserBookingRestrictionResponse
            {
                RestrictionId =
                    restriction.Id,

                UserId =
                    restriction.UserId,

                Type =
                    restriction.Type.ToString(),

                Status =
                    restriction.Status.ToString(),

                Reason =
                    restriction.Reason,

                CancellationCountSnapshot =
                    restriction.CancellationCountSnapshot,

                RestrictedFrom =
                    restriction.RestrictedFrom,

                RestrictedUntil =
                    restriction.RestrictedUntil,

                CreatedBySystem =
                    restriction.CreatedBySystem,

                CreatedByAdminId =
                    restriction.CreatedByAdminId,

                CreatedByAdminName =
                    restriction.CreatedByAdmin == null
                        ? null
                        : BuildFullName(
                            restriction.CreatedByAdmin.FirstName,
                            restriction.CreatedByAdmin.LastName,
                            restriction.CreatedByAdmin.Email),

                CreatedAt =
                    restriction.CreatedAt,

                UpdatedAt =
                    restriction.UpdatedAt,

                RemovedByAdminId =
                    restriction.RemovedByAdminId,

                RemovedByAdminName =
                    restriction.RemovedByAdmin == null
                        ? null
                        : BuildFullName(
                            restriction.RemovedByAdmin.FirstName,
                            restriction.RemovedByAdmin.LastName,
                            restriction.RemovedByAdmin.Email),

                RemovedAt =
                    restriction.RemovedAt,

                RemovalNote =
                    restriction.RemovalNote
            };
        }

        private static void ValidateUserIdentifier(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain a valid user identifier.");
            }
        }

        private static void ValidateRestrictionIdentifier(
            Guid restrictionId)
        {
            if (restrictionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The user booking restriction identifier is invalid.");
            }
        }

        private static string NormalizeReason(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "The restriction reason is required.");
            }

            var normalizedValue =
                value.Trim();

            if (normalizedValue.Length >
                MaximumReasonLength)
            {
                throw new ArgumentException(
                    $"The restriction reason cannot exceed {MaximumReasonLength} characters.");
            }

            return normalizedValue;
        }

        private static string? NormalizeOptionalText(
            string? value,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            var normalizedValue =
                value.Trim();

            if (normalizedValue.Length >
                maximumLength)
            {
                throw new ArgumentException(
                    $"The value cannot exceed {maximumLength} characters.");
            }

            return normalizedValue;
        }

        private static string BuildFullName(
            string? firstName,
            string? lastName,
            string? fallback)
        {
            var fullName =
                string.Join(
                    " ",
                    new[]
                    {
                        firstName,
                        lastName
                    }
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(
                        value =>
                            value!.Trim()));

            if (!string.IsNullOrWhiteSpace(
                    fullName))
            {
                return fullName;
            }

            return fallback
                ??
                "Unknown User";
        }
    }
}