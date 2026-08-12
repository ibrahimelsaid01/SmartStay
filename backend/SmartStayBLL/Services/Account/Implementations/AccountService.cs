using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AccountService
        : IAccountService
    {
        private const string RequiredConfirmation =
            "DEACTIVATE";

        private readonly SmartStayDbContext _dbContext;

        private readonly UserManager<ApplicationUser>
            _userManager;

        private readonly IRefreshTokenService
            _refreshTokenService;

        public AccountService(
            SmartStayDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IRefreshTokenService refreshTokenService)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                userManager);

            ArgumentNullException.ThrowIfNull(
                refreshTokenService);

            _dbContext =
                dbContext;

            _userManager =
                userManager;

            _refreshTokenService =
                refreshTokenService;
        }

        public async Task<AccountDeactivationResponse>
            DeactivateAsync(
                Guid userId,
                DeactivateAccountRequest request,
                string? ipAddress,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                userId);

            ArgumentNullException.ThrowIfNull(
                request);

            ValidateConfirmation(
                request.Confirmation);

            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            var user =
                await _dbContext.Users
                    .Include(item =>
                        item.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile!.Properties)
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == userId,
                        cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "The user account was not found.");
            }

            /*
             * Admin accounts must not deactivate
             * themselves through the public account API.
             */
            var isAdministrator =
                await _userManager.IsInRoleAsync(
                    user,
                    RoleNames.Admin);

            if (isAdministrator)
            {
                throw new InvalidOperationException(
                    "Administrator accounts cannot be self-deactivated.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            /*
             * Idempotent protection in case two requests
             * arrive at nearly the same time.
             */
            if (!user.IsActive)
            {
                await _refreshTokenService
                    .RevokeAllForUserAsync(
                        userId,
                        ipAddress,
                        "Account is deactivated.",
                        cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return new AccountDeactivationResponse
                {
                    IsDeactivated =
                        true,

                    DeactivatedAt =
                        user.UpdatedAt
                        ??
                        currentTime,

                    UnpublishedPropertiesCount =
                        0,

                    Message =
                        "The account is already deactivated."
                };
            }

            /*
             * Do not silently cancel a guest's reservation.
             *
             * An unexpired Pending booking may still be
             * completing payment, while a Confirmed booking
             * represents an active reservation.
             */
            var hasActiveGuestBookings =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .AnyAsync(
                        booking =>
                            booking.GuestUserId ==
                                userId
                            &&
                            (
                                booking.Status ==
                                    BookingStatus.Confirmed
                                ||
                                (
                                    booking.Status ==
                                        BookingStatus.Pending
                                    &&
                                    booking.ExpiresAt
                                        .HasValue
                                    &&
                                    booking.ExpiresAt.Value >
                                        currentTime
                                )
                            ),
                        cancellationToken);

            if (hasActiveGuestBookings)
            {
                throw new InvalidOperationException(
                    "The account cannot be deactivated while you have pending or confirmed bookings.");
            }

            /*
             * A Host must also finish all active guest
             * reservations before deactivating the account.
             */
            if (user.HostProfile is not null)
            {
                var hostProfileId =
                    user.HostProfile.Id;

                var hasActiveHostBookings =
                    await _dbContext.Bookings
                        .AsNoTracking()
                        .AnyAsync(
                            booking =>
                                booking.Property
                                    .HostProfileId ==
                                        hostProfileId
                                &&
                                (
                                    booking.Status ==
                                        BookingStatus.Confirmed
                                    ||
                                    (
                                        booking.Status ==
                                            BookingStatus.Pending
                                        &&
                                        booking.ExpiresAt
                                            .HasValue
                                        &&
                                        booking.ExpiresAt
                                            .Value >
                                                currentTime
                                    )
                                ),
                            cancellationToken);

                if (hasActiveHostBookings)
                {
                    throw new InvalidOperationException(
                        "The host account cannot be deactivated while its properties have pending or confirmed bookings.");
                }
            }

            var publishedProperties =
                user.HostProfile?.Properties
                    .Where(property =>
                        property.Status ==
                            PropertyStatus.Published)
                    .ToList()
                ??
                new List<Property>();

            /*
             * Keep the property data, images, documents,
             * and booking history, but remove the listings
             * from public search.
             */
            foreach (var property in publishedProperties)
            {
                property.Status =
                    PropertyStatus.Unpublished;

                property.UpdatedAt =
                    currentTime;
            }

            user.IsActive =
                false;

            /*
             * UpdatedAt acts as the deactivation timestamp
             * without requiring a new database column.
             */
            user.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            /*
             * Revoke all active sessions.
             *
             * This uses the same scoped DbContext and remains
             * inside the current database transaction.
             */
            await _refreshTokenService
                .RevokeAllForUserAsync(
                    userId,
                    ipAddress,
                    "Account deactivated by the user.",
                    cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new AccountDeactivationResponse
            {
                IsDeactivated =
                    true,

                DeactivatedAt =
                    currentTime,

                UnpublishedPropertiesCount =
                    publishedProperties.Count,

                Message =
                    "The account was deactivated successfully."
            };
        }

        private static void ValidateUserId(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user identifier is invalid.");
            }
        }

        private static void ValidateConfirmation(
            string? confirmation)
        {
            if (!string.Equals(
                    confirmation?.Trim(),
                    RequiredConfirmation,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Enter {RequiredConfirmation} to confirm account deactivation.");
            }
        }
    }
}