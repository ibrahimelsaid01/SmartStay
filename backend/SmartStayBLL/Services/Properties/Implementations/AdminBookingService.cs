using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminBookingService
        : IAdminBookingService
    {
        private const int MaximumPageSize = 100;

        private readonly SmartStayDbContext _dbContext;

        public AdminBookingService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext = dbContext;
        }

        /*
         * =====================================================
         * Admin booking list
         * =====================================================
         */

        public async Task<AdminBookingsResponse>
            GetBookingsAsync(
                AdminBookingSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ValidateSearchRequest(
                request);

            var query =
                _dbContext.Bookings
                    .AsNoTracking()
                    .AsQueryable();

            if (request.Status.HasValue)
            {
                query =
                    query.Where(booking =>
                        booking.Status ==
                            request.Status.Value);
            }

            if (request.PropertyId.HasValue)
            {
                query =
                    query.Where(booking =>
                        booking.PropertyId ==
                            request.PropertyId.Value);
            }

            if (request.GuestUserId.HasValue)
            {
                query =
                    query.Where(booking =>
                        booking.GuestUserId ==
                            request.GuestUserId.Value);
            }

            if (request.HostUserId.HasValue)
            {
                query =
                    query.Where(booking =>
                        booking.Property
                            .HostProfile
                            .UserId ==
                        request.HostUserId.Value);
            }

            if (request.CheckInFrom.HasValue)
            {
                query =
                    query.Where(booking =>
                        booking.CheckInDate >=
                            request.CheckInFrom.Value);
            }

            if (request.CheckInTo.HasValue)
            {
                query =
                    query.Where(booking =>
                        booking.CheckInDate <=
                            request.CheckInTo.Value);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var rawItems =
                await query
                    .OrderByDescending(booking =>
                        booking.CreatedAt)
                    .Skip(
                        (request.Page - 1)
                        *
                        request.PageSize)
                    .Take(request.PageSize)
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
                            booking.CancellationPolicySnapshot,
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
                                booking.GuestUser.PhoneNumber,

                            GuestIsActive =
                                booking.GuestUser.IsActive,

                            HostUserId =
                                booking.Property
                                    .HostProfile
                                    .User.Id,

                            HostFirstName =
                                booking.Property
                                    .HostProfile
                                    .User.FirstName,

                            HostLastName =
                                booking.Property
                                    .HostProfile
                                    .User.LastName,

                            HostEmail =
                                booking.Property
                                    .HostProfile
                                    .User.Email,

                            HostPhoneNumber =
                                booking.Property
                                    .HostProfile
                                    .User.PhoneNumber,

                            HostIsActive =
                                booking.Property
                                    .HostProfile
                                    .User.IsActive
                        })
                    .ToListAsync(
                        cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            var currentDate =
                GetUtcDate(currentTime);

            var items =
                rawItems
                    .Select(item =>
                    {
                        ValidateCancellationPolicySnapshot(
                            item.CancellationPolicySnapshot);

                        var guestFirstName =
                            item.GuestFirstName
                            ?? string.Empty;

                        var guestLastName =
                            item.GuestLastName
                            ?? string.Empty;

                        var hostFirstName =
                            item.HostFirstName
                            ?? string.Empty;

                        var hostLastName =
                            item.HostLastName
                            ?? string.Empty;

                        return new AdminBookingListItemResponse
                        {
                            BookingId =
                                item.BookingId,

                            Property =
                                new AdminBookingPropertyResponse
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
                                new AdminBookingUserResponse
                                {
                                    UserId =
                                        item.GuestUserId,

                                    FirstName =
                                        guestFirstName,

                                    LastName =
                                        guestLastName,

                                    FullName =
                                        BuildFullName(
                                            guestFirstName,
                                            guestLastName),

                                    Email =
                                        item.GuestEmail
                                        ?? string.Empty,

                                    PhoneNumber =
                                        item.GuestPhoneNumber,

                                    IsActive =
                                        item.GuestIsActive
                                },

                            Host =
                                new AdminBookingUserResponse
                                {
                                    UserId =
                                        item.HostUserId,

                                    FirstName =
                                        hostFirstName,

                                    LastName =
                                        hostLastName,

                                    FullName =
                                        BuildFullName(
                                            hostFirstName,
                                            hostLastName),

                                    Email =
                                        item.HostEmail
                                        ?? string.Empty,

                                    PhoneNumber =
                                        item.HostPhoneNumber,

                                    IsActive =
                                        item.HostIsActive
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

                            CancellationPolicy =
                                item
                                    .CancellationPolicySnapshot
                                    .ToString(),

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
                        totalCount
                        /
                        (double)request.PageSize);

            return new AdminBookingsResponse
            {
                Items =
                    items,

                Page =
                    request.Page,

                PageSize =
                    request.PageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    totalPages,

                AppliedStatusFilter =
                    request.Status?.ToString(),

                AppliedPropertyIdFilter =
                    request.PropertyId,

                AppliedGuestUserIdFilter =
                    request.GuestUserId,

                AppliedHostUserIdFilter =
                    request.HostUserId,

                AppliedCheckInFromFilter =
                    request.CheckInFrom,

                AppliedCheckInToFilter =
                    request.CheckInTo
            };
        }

        /*
         * =====================================================
         * Admin booking summary
         * =====================================================
         */

        public async Task<AdminBookingSummaryResponse>
            GetSummaryAsync(
                CancellationToken cancellationToken = default)
        {
            var query =
                _dbContext.Bookings
                    .AsNoTracking();

            var currentTime =
                DateTimeOffset.UtcNow;

            var currentDate =
                GetUtcDate(currentTime);

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
             * Upcoming includes:
             *
             * - Confirmed future bookings.
             * - Pending future bookings whose payment
             *   window is still active.
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
             * Pending, Cancelled and Expired bookings are not
             * included in confirmed/completed financial
             * snapshots.
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

                            ConfirmedGrossAmount =
                                group.Sum(booking =>
                                    booking.Status ==
                                        BookingStatus.Confirmed
                                        ? booking.TotalAmount
                                        : 0m),

                            ConfirmedServiceFees =
                                group.Sum(booking =>
                                    booking.Status ==
                                        BookingStatus.Confirmed
                                        ? booking.ServiceFee
                                        : 0m),

                            CompletedGrossAmount =
                                group.Sum(booking =>
                                    booking.Status ==
                                        BookingStatus.Completed
                                        ? booking.TotalAmount
                                        : 0m),

                            CompletedServiceFees =
                                group.Sum(booking =>
                                    booking.Status ==
                                        BookingStatus.Completed
                                        ? booking.ServiceFee
                                        : 0m)
                        })
                    .OrderBy(item =>
                        item.Currency)
                    .ToListAsync(
                        cancellationToken);

            var amountsByCurrency =
                rawAmounts
                    .Select(item =>
                        new AdminBookingAmountByCurrencyResponse
                        {
                            Currency =
                                NormalizeCurrency(
                                    item.Currency),

                            ConfirmedGrossAmount =
                                RoundMoney(
                                    item.ConfirmedGrossAmount),

                            ConfirmedServiceFees =
                                RoundMoney(
                                    item.ConfirmedServiceFees),

                            CompletedGrossAmount =
                                RoundMoney(
                                    item.CompletedGrossAmount),

                            CompletedServiceFees =
                                RoundMoney(
                                    item.CompletedServiceFees)
                        })
                    .ToList();

            return new AdminBookingSummaryResponse
            {
                /*
                 * Includes every persisted status,
                 * including Expired.
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
         * Admin booking details
         * =====================================================
         */

        public async Task<AdminBookingDetailsResponse>
            GetBookingByIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            ValidateBookingIdentifier(
                bookingId);

            var booking =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(booking =>
                        booking.Property)
                    .ThenInclude(property =>
                        property.Images)
                    .Include(booking =>
                        booking.Property)
                    .ThenInclude(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .Include(booking =>
                        booking.GuestUser)
                    .SingleOrDefaultAsync(
                        booking =>
                            booking.Id == bookingId,
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

            return new AdminBookingDetailsResponse
            {
                BookingId =
                    booking.Id,

                Property =
                    MapProperty(
                        booking.Property),

                Guest =
                    MapUser(
                        booking.GuestUser),

                Host =
                    MapUser(
                        booking.Property
                            .HostProfile
                            .User),

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
         * Mapping helpers
         * =====================================================
         */

        private static AdminBookingPropertyResponse
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

            return new AdminBookingPropertyResponse
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

        private static AdminBookingUserResponse
            MapUser(
                ApplicationUser user)
        {
            var firstName =
                user.FirstName
                ?? string.Empty;

            var lastName =
                user.LastName
                ?? string.Empty;

            return new AdminBookingUserResponse
            {
                UserId =
                    user.Id,

                FirstName =
                    firstName,

                LastName =
                    lastName,

                FullName =
                    BuildFullName(
                        firstName,
                        lastName),

                Email =
                    user.Email
                    ?? string.Empty,

                PhoneNumber =
                    user.PhoneNumber,

                IsActive =
                    user.IsActive
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

        private static void ValidateSearchRequest(
            AdminBookingSearchRequest request)
        {
            ValidatePagination(
                request.Page,
                request.PageSize);

            ValidateOptionalBookingStatus(
                request.Status);

            ValidateOptionalIdentifier(
                request.PropertyId,
                "The property identifier is invalid.");

            ValidateOptionalIdentifier(
                request.GuestUserId,
                "The guest user identifier is invalid.");

            ValidateOptionalIdentifier(
                request.HostUserId,
                "The host user identifier is invalid.");

            if (request.CheckInFrom.HasValue
                &&
                request.CheckInTo.HasValue
                &&
                request.CheckInFrom.Value >
                    request.CheckInTo.Value)
            {
                throw new ArgumentException(
                    "The check-in start date cannot be after the check-in end date.");
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

        private static void ValidateOptionalIdentifier(
            Guid? identifier,
            string errorMessage)
        {
            if (identifier.HasValue
                &&
                identifier.Value == Guid.Empty)
            {
                throw new ArgumentException(
                    errorMessage);
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