using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed partial class HostBookingService
        : IHostBookingService
    {
        private const int MaximumPageSize = 100;

        private readonly SmartStayDbContext _dbContext;

        public HostBookingService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext = dbContext;
        }

        /*
         * =====================================================
         * Host booking list
         * =====================================================
         */

        public async Task<HostBookingsResponse>
            GetBookingsAsync(
                Guid hostUserId,
                int page,
                int pageSize,
                BookingStatus? status,
                CancellationToken cancellationToken = default)
        {
            ValidateHostUserIdentifier(
                hostUserId);

            ValidatePagination(
                page,
                pageSize);

            ValidateOptionalBookingStatus(
                status);

            await EnsureApprovedActiveHostAsync(
                hostUserId,
                cancellationToken);

            /*
             * The host receives only bookings related
             * to properties owned by their HostProfile.
             */
            var query =
                _dbContext.Bookings
                    .AsNoTracking()
                    .Where(booking =>
                        booking.Property
                            .HostProfile
                            .UserId == hostUserId);

            if (status.HasValue)
            {
                query =
                    query.Where(booking =>
                        booking.Status ==
                            status.Value);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var rawItems =
                await query
                    .OrderByDescending(booking =>
                        booking.CreatedAt)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(booking =>
                        new
                        {
                            BookingId =
                                booking.Id,

                            booking.CheckInDate,
                            booking.CheckOutDate,
                            booking.Nights,
                            booking.GuestsCount,
                            booking.Subtotal,
                            booking.ServiceFee,
                            booking.TotalAmount,
                            booking.Currency,
                            booking.Status,

                            booking.ExpiresAt,
                            booking.CreatedAt,
                            booking.ConfirmedAt,
                            booking.CancelledAt,
                            booking.ExpiredAt,
                            booking.CompletedAt,

                            PropertyId =
                                booking.Property.Id,

                            PropertyTitle =
                                booking.Property.Title,

                            PropertyCountry =
                                booking.Property.Country,

                            PropertyCity =
                                booking.Property.City,

                            CoverImageUrl =
                                booking.Property.Images
                                    .OrderByDescending(image =>
                                        image.IsCover)
                                    .ThenBy(image =>
                                        image.DisplayOrder)
                                    .Select(image =>
                                        image.Url)
                                    .FirstOrDefault(),

                            GuestUserId =
                                booking.GuestUser.Id,

                            GuestFirstName =
                                booking.GuestUser.FirstName,

                            GuestLastName =
                                booking.GuestUser.LastName,

                            GuestEmail =
                                booking.GuestUser.Email,

                            GuestPhoneNumber =
                                booking.GuestUser.PhoneNumber
                        })
                    .ToListAsync(
                        cancellationToken);

            /*
             * Capture the time once so all rows are evaluated
             * against the same timestamp.
             */
            var currentTime =
                DateTimeOffset.UtcNow;

            var currentDate =
                GetUtcDate(currentTime);

            var items =
                rawItems
                    .Select(item =>
                    {
                        var firstName =
                            item.GuestFirstName
                            ?? string.Empty;

                        var lastName =
                            item.GuestLastName
                            ?? string.Empty;

                        return new HostBookingListItemResponse
                        {
                            BookingId =
                                item.BookingId,

                            Property =
                                new HostBookingPropertyResponse
                                {
                                    Id =
                                        item.PropertyId,

                                    Title =
                                        item.PropertyTitle,

                                    Country =
                                        item.PropertyCountry
                                        ?? string.Empty,

                                    City =
                                        item.PropertyCity
                                        ?? string.Empty,

                                    CoverImageUrl =
                                        item.CoverImageUrl
                                },

                            Guest =
                                new HostBookingGuestResponse
                                {
                                    UserId =
                                        item.GuestUserId,

                                    FirstName =
                                        firstName,

                                    LastName =
                                        lastName,

                                    FullName =
                                        BuildFullName(
                                            firstName,
                                            lastName),

                                    Email =
                                        item.GuestEmail
                                        ?? string.Empty,

                                    PhoneNumber =
                                        item.GuestPhoneNumber
                                },

                            CheckInDate =
                                item.CheckInDate,

                            CheckOutDate =
                                item.CheckOutDate,

                            Nights =
                                item.Nights,

                            GuestsCount =
                                item.GuestsCount,

                            Subtotal =
                                item.Subtotal,

                            ServiceFee =
                                item.ServiceFee,

                            TotalAmount =
                                item.TotalAmount,

                            Currency =
                                NormalizeCurrency(
                                    item.Currency),

                            Status =
                                item.Status.ToString(),

                            IsUpcoming =
                                IsUpcomingBooking(
                                    item.Status,
                                    item.CheckInDate,
                                    item.ExpiresAt,
                                    currentDate,
                                    currentTime),

                            IsCurrentlyStaying =
                                IsCurrentlyStaying(
                                    item.Status,
                                    item.CheckInDate,
                                    item.CheckOutDate,
                                    currentDate),

                            IsPaymentWindowExpired =
                                IsPaymentWindowExpired(
                                    item.Status,
                                    item.ExpiresAt,
                                    currentTime),

                            ExpiresAt =
                                item.ExpiresAt,

                            CreatedAt =
                                item.CreatedAt,

                            ConfirmedAt =
                                item.ConfirmedAt,

                            CancelledAt =
                                item.CancelledAt,

                            ExpiredAt =
                                item.ExpiredAt,

                            CompletedAt =
                                item.CompletedAt
                        };
                    })
                    .ToList();

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount /
                        (double)pageSize);

            return new HostBookingsResponse
            {
                Items =
                    items,

                Page =
                    page,

                PageSize =
                    pageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    totalPages,

                AppliedStatusFilter =
                    status?.ToString()
            };
        }

        /*
         * =====================================================
         * Host dashboard booking summary
         * =====================================================
         */

        public async Task<HostBookingSummaryResponse>
            GetSummaryAsync(
                Guid hostUserId,
                CancellationToken cancellationToken = default)
        {
            ValidateHostUserIdentifier(
                hostUserId);

            await EnsureApprovedActiveHostAsync(
                hostUserId,
                cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            var currentDate =
                GetUtcDate(currentTime);

            var query =
                _dbContext.Bookings
                    .AsNoTracking()
                    .Where(booking =>
                        booking.Property
                            .HostProfile
                            .UserId == hostUserId);

            /*
             * Count every persisted booking status,
             * including Expired.
             */
            var statusCounts =
                await query
                    .GroupBy(booking =>
                        booking.Status)
                    .Select(group =>
                        new
                        {
                            Status =
                                group.Key,

                            Count =
                                group.Count()
                        })
                    .ToListAsync(
                        cancellationToken);

            var countsByStatus =
                statusCounts.ToDictionary(
                    item => item.Status,
                    item => item.Count);

            var pendingCount =
                GetStatusCount(
                    countsByStatus,
                    BookingStatus.Pending);

            var confirmedCount =
                GetStatusCount(
                    countsByStatus,
                    BookingStatus.Confirmed);

            var cancelledCount =
                GetStatusCount(
                    countsByStatus,
                    BookingStatus.Cancelled);

            var completedCount =
                GetStatusCount(
                    countsByStatus,
                    BookingStatus.Completed);

            var expiredCount =
                GetStatusCount(
                    countsByStatus,
                    BookingStatus.Expired);

            /*
             * Upcoming means:
             *
             * 1. Confirmed with a future check-in.
             *
             * OR
             *
             * 2. Pending, payment window still active,
             *    and check-in is in the future.
             *
             * A stale Pending booking is not upcoming even if
             * the background process has not changed its status
             * to Expired yet.
             */
            var upcomingBookings =
                await query.CountAsync(
                    booking =>
                        booking.CheckInDate >
                            currentDate
                        &&
                        (
                            booking.Status ==
                                BookingStatus.Confirmed
                            ||
                            (
                                booking.Status ==
                                    BookingStatus.Pending
                                &&
                                booking.ExpiresAt.HasValue
                                &&
                                booking.ExpiresAt.Value >
                                    currentTime
                            )
                        ),
                    cancellationToken);

            /*
             * A current stay must be Confirmed.
             *
             * Check-in is inclusive.
             * Check-out is exclusive.
             */
            var currentStays =
                await query.CountAsync(
                    booking =>
                        booking.Status ==
                            BookingStatus.Confirmed
                        &&
                        booking.CheckInDate <=
                            currentDate
                        &&
                        booking.CheckOutDate >
                            currentDate,
                    cancellationToken);

            /*
             * Amounts remain grouped by currency.
             *
             * Only Confirmed and Completed bookings are
             * included in these financial snapshots.
             */
            var rawAmounts =
                await query
                    .Where(booking =>
                        booking.Status ==
                            BookingStatus.Confirmed
                        ||
                        booking.Status ==
                            BookingStatus.Completed)
                    .GroupBy(booking =>
                        booking.Currency)
                    .Select(group =>
                        new
                        {
                            Currency =
                                group.Key,

                            ConfirmedSubtotal =
                                group
                                    .Where(booking =>
                                        booking.Status ==
                                            BookingStatus.Confirmed)
                                    .Sum(booking =>
                                        (decimal?)
                                            booking.Subtotal)
                                ?? 0m,

                            CompletedSubtotal =
                                group
                                    .Where(booking =>
                                        booking.Status ==
                                            BookingStatus.Completed)
                                    .Sum(booking =>
                                        (decimal?)
                                            booking.Subtotal)
                                ?? 0m
                        })
                    .OrderBy(item =>
                        item.Currency)
                    .ToListAsync(
                        cancellationToken);

            var amountsByCurrency =
                rawAmounts
                    .Select(item =>
                        new HostBookingAmountByCurrencyResponse
                        {
                            Currency =
                                NormalizeCurrency(
                                    item.Currency),

                            ConfirmedBookingSubtotal =
                                RoundMoney(
                                    item.ConfirmedSubtotal),

                            CompletedBookingSubtotal =
                                RoundMoney(
                                    item.CompletedSubtotal)
                        })
                    .ToList();

            return new HostBookingSummaryResponse
            {
                /*
                 * Sum all statuses instead of manually adding
                 * selected statuses.
                 *
                 * This automatically includes Expired and any
                 * future persisted status.
                 */
                TotalBookings =
                    statusCounts.Sum(item =>
                        item.Count),

                PendingBookings =
                    pendingCount,

                ConfirmedBookings =
                    confirmedCount,

                CancelledBookings =
                    cancelledCount,

                CompletedBookings =
                    completedCount,

                ExpiredBookings =
                    expiredCount,

                UpcomingBookings =
                    upcomingBookings,

                CurrentStays =
                    currentStays,

                AmountsByCurrency =
                    amountsByCurrency
            };
        }

        /*
         * =====================================================
         * Host booking details
         * =====================================================
         */

        public async Task<HostBookingDetailsResponse>
            GetBookingByIdAsync(
                Guid hostUserId,
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            ValidateHostUserIdentifier(
                hostUserId);

            ValidateBookingIdentifier(
                bookingId);

            await EnsureApprovedActiveHostAsync(
                hostUserId,
                cancellationToken);

            /*
             * Ownership is included in the database query.
             *
             * A booking belonging to another host returns
             * the same response as a missing booking.
             */
            var booking =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(booking =>
                        booking.Property)
                    .ThenInclude(property =>
                        property.Images)
                    .Include(booking =>
                        booking.GuestUser)
                    .SingleOrDefaultAsync(
                        booking =>
                            booking.Id == bookingId
                            &&
                            booking.Property
                                .HostProfile
                                .UserId == hostUserId,
                        cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    "The booking was not found.");
            }

            ValidateCancellationPolicySnapshot(
                booking.CancellationPolicySnapshot);

            var currentTime =
                DateTimeOffset.UtcNow;

            var currentDate =
                GetUtcDate(currentTime);

            return new HostBookingDetailsResponse
            {
                BookingId =
                    booking.Id,

                Property =
                    MapProperty(
                        booking.Property),

                Guest =
                    MapGuest(
                        booking.GuestUser),

                CheckInDate =
                    booking.CheckInDate,

                CheckOutDate =
                    booking.CheckOutDate,

                GuestsCount =
                    booking.GuestsCount,

                Nights =
                    booking.Nights,

                PricePerNight =
                    booking.PricePerNight,

                Subtotal =
                    booking.Subtotal,

                ServiceFee =
                    booking.ServiceFee,

                TotalAmount =
                    booking.TotalAmount,

                Currency =
                    NormalizeCurrency(
                        booking.Currency),

                CancellationPolicy =
                    booking
                        .CancellationPolicySnapshot
                        .ToString(),

                Status =
                    booking.Status.ToString(),

                IsUpcoming =
                    IsUpcomingBooking(
                        booking.Status,
                        booking.CheckInDate,
                        booking.ExpiresAt,
                        currentDate,
                        currentTime),

                IsCurrentlyStaying =
                    IsCurrentlyStaying(
                        booking.Status,
                        booking.CheckInDate,
                        booking.CheckOutDate,
                        currentDate),

                IsPaymentWindowExpired =
                    IsPaymentWindowExpired(
                        booking.Status,
                        booking.ExpiresAt,
                        currentTime),

                CancellationReason =
                    booking.CancellationReason,

                ExpiresAt =
                    booking.ExpiresAt,

                CreatedAt =
                    booking.CreatedAt,

                UpdatedAt =
                    booking.UpdatedAt,

                ConfirmedAt =
                    booking.ConfirmedAt,

                CancelledAt =
                    booking.CancelledAt,

                ExpiredAt =
                    booking.ExpiredAt,

                CompletedAt =
                    booking.CompletedAt
            };
        }

        /*
         * =====================================================
         * Host validation
         * =====================================================
         */

        private async Task EnsureApprovedActiveHostAsync(
            Guid hostUserId,
            CancellationToken cancellationToken)
        {
            var host =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .Where(hostProfile =>
                        hostProfile.UserId ==
                            hostUserId)
                    .Select(hostProfile =>
                        new
                        {
                            hostProfile.Status,

                            UserIsActive =
                                hostProfile.User.IsActive
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (host is null)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated host profile was not found.");
            }

            if (!host.UserIsActive)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated host account is inactive.");
            }

            if (host.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can access host bookings.");
            }
        }

        /*
         * =====================================================
         * Mapping helpers
         * =====================================================
         */

        private static HostBookingPropertyResponse
            MapProperty(
                Property property)
        {
            var coverImageUrl =
                property.Images
                    .OrderByDescending(image =>
                        image.IsCover)
                    .ThenBy(image =>
                        image.DisplayOrder)
                    .Select(image =>
                        image.Url)
                    .FirstOrDefault();

            return new HostBookingPropertyResponse
            {
                Id =
                    property.Id,

                Title =
                    property.Title,

                Country =
                    property.Country
                    ?? string.Empty,

                City =
                    property.City
                    ?? string.Empty,

                CoverImageUrl =
                    coverImageUrl
            };
        }

        private static HostBookingGuestResponse
            MapGuest(
                ApplicationUser guest)
        {
            var firstName =
                guest.FirstName
                ?? string.Empty;

            var lastName =
                guest.LastName
                ?? string.Empty;

            return new HostBookingGuestResponse
            {
                UserId =
                    guest.Id,

                FirstName =
                    firstName,

                LastName =
                    lastName,

                FullName =
                    BuildFullName(
                        firstName,
                        lastName),

                Email =
                    guest.Email
                    ?? string.Empty,

                PhoneNumber =
                    guest.PhoneNumber
            };
        }

        private static string BuildFullName(
            string firstName,
            string lastName)
        {
            return string.Join(
                ' ',
                new[]
                {
                    firstName.Trim(),
                    lastName.Trim()
                }
                .Where(namePart =>
                    !string.IsNullOrWhiteSpace(
                        namePart)));
        }

        /*
         * =====================================================
         * Booking lifecycle helpers
         * =====================================================
         */

        private static bool IsUpcomingBooking(
            BookingStatus status,
            DateOnly checkInDate,
            DateTimeOffset? expiresAt,
            DateOnly currentDate,
            DateTimeOffset currentTime)
        {
            if (checkInDate <= currentDate)
            {
                return false;
            }

            return status switch
            {
                BookingStatus.Confirmed =>
                    true,

                BookingStatus.Pending =>
                    expiresAt.HasValue
                    &&
                    expiresAt.Value > currentTime,

                _ =>
                    false
            };
        }

        private static bool IsCurrentlyStaying(
            BookingStatus status,
            DateOnly checkInDate,
            DateOnly checkOutDate,
            DateOnly currentDate)
        {
            return status ==
                       BookingStatus.Confirmed
                   &&
                   checkInDate <= currentDate
                   &&
                   checkOutDate > currentDate;
        }

        private static bool IsPaymentWindowExpired(
            BookingStatus status,
            DateTimeOffset? expiresAt,
            DateTimeOffset currentTime)
        {
            if (status == BookingStatus.Expired)
            {
                return true;
            }

            return status ==
                       BookingStatus.Pending
                   &&
                   (
                       !expiresAt.HasValue
                       ||
                       expiresAt.Value <= currentTime
                   );
        }

        private static int GetStatusCount(
            IReadOnlyDictionary<BookingStatus, int>
                statusCounts,
            BookingStatus status)
        {
            return statusCounts.TryGetValue(
                status,
                out var count)
                    ? count
                    : 0;
        }

        /*
         * =====================================================
         * Validation helpers
         * =====================================================
         */

        private static void ValidateHostUserIdentifier(
            Guid hostUserId)
        {
            if (hostUserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain a valid user identifier.");
            }
        }

        private static void ValidateBookingIdentifier(
            Guid bookingId)
        {
            if (bookingId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The booking identifier is invalid.");
            }
        }

        private static void ValidatePagination(
            int page,
            int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentException(
                    "The page number must be greater than or equal to 1.");
            }

            if (pageSize is < 1 or > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"The page size must be between 1 and " +
                    $"{MaximumPageSize}.");
            }
        }

        private static void ValidateOptionalBookingStatus(
            BookingStatus? status)
        {
            if (status.HasValue
                &&
                !Enum.IsDefined(status.Value))
            {
                throw new ArgumentException(
                    "The selected booking status is invalid.");
            }
        }

        private static void
            ValidateCancellationPolicySnapshot(
                CancellationPolicyType policy)
        {
            if (!Enum.IsDefined(policy))
            {
                throw new InvalidOperationException(
                    "The booking cancellation policy snapshot is invalid.");
            }
        }

        /*
         * =====================================================
         * General helpers
         * =====================================================
         */

        private static DateOnly GetUtcDate(
            DateTimeOffset dateTime)
        {
            return DateOnly.FromDateTime(
                dateTime.UtcDateTime);
        }

        private static string NormalizeCurrency(
            string? currency)
        {
            return currency?
                       .Trim()
                       .ToUpperInvariant()
                   ?? string.Empty;
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }
    }
}