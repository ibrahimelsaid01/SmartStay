using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed partial class BookingService : IBookingService
    {
        private const decimal ServiceFeeRate = 0.10m;

        private const int PendingBookingExpirationMinutes = 15;

        private const int MinimumGuestsCount = 1;
        private const int MaximumGuestsCount = 20;
        private const int MaximumPageSize = 100;
        private const int MaximumCancellationReasonLength = 500;

        private const string AutomaticRefundFailureMessage =
            "The refund could not be processed automatically. " +
            "It requires retry or admin review.";

        private readonly SmartStayDbContext _dbContext;

        private readonly IPaymentRefundService _paymentRefundService;

        private readonly IUserBookingRestrictionService _userBookingRestrictionService;

        private readonly ILogger<BookingService> _logger;

        public BookingService(
            SmartStayDbContext dbContext,
            IPaymentRefundService paymentRefundService,
            IUserBookingRestrictionService userBookingRestrictionService,
            ILogger<BookingService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(paymentRefundService);
            ArgumentNullException.ThrowIfNull(userBookingRestrictionService);
            ArgumentNullException.ThrowIfNull(logger);

            _dbContext =
                dbContext;

            _paymentRefundService =
                paymentRefundService;

            _userBookingRestrictionService =
                userBookingRestrictionService;

            _logger =
                logger;
        }

        /*
         * =====================================================
         * Availability
         * =====================================================
         */

        public async Task<PropertyAvailabilityResponse>
            CheckAvailabilityAsync(
                Guid propertyId,
                BookingPeriodRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidatePropertyIdentifier(propertyId);

            ArgumentNullException.ThrowIfNull(request);

            var period = ValidateBookingPeriod(
                request.CheckInDate,
                request.CheckOutDate);

            ValidateGuestsCount(request.GuestsCount);

            var property =
                await GetBookablePropertyAsync(
                    propertyId,
                    cancellationToken);

            var maxGuests =
                property.MaxGuests
                ?? throw new InvalidOperationException(
                    "The property guest capacity is not configured.");

            if (request.GuestsCount > maxGuests)
            {
                return new PropertyAvailabilityResponse
                {
                    PropertyId = property.Id,
                    CheckInDate = period.CheckInDate,
                    CheckOutDate = period.CheckOutDate,
                    GuestsCount = request.GuestsCount,
                    Nights = period.Nights,
                    IsAvailable = false,
                    Message =
                        $"The property accommodates a maximum of " +
                        $"{maxGuests} guests."
                };
            }

            var hasConflictingBooking =
                await HasConflictingBookingAsync(
                    property.Id,
                    period.CheckInDate,
                    period.CheckOutDate,
                    cancellationToken);

            return new PropertyAvailabilityResponse
            {
                PropertyId = property.Id,
                CheckInDate = period.CheckInDate,
                CheckOutDate = period.CheckOutDate,
                GuestsCount = request.GuestsCount,
                Nights = period.Nights,
                IsAvailable = !hasConflictingBooking,
                Message = hasConflictingBooking
                    ? "The property is not available for the selected dates."
                    : "The property is available for the selected dates."
            };
        }

        /*
         * =====================================================
         * Quote
         * =====================================================
         */

        public async Task<BookingQuoteResponse>
            GetQuoteAsync(
                Guid propertyId,
                BookingPeriodRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidatePropertyIdentifier(propertyId);

            ArgumentNullException.ThrowIfNull(request);

            var period = ValidateBookingPeriod(
                request.CheckInDate,
                request.CheckOutDate);

            ValidateGuestsCount(request.GuestsCount);

            var property =
                await GetBookablePropertyAsync(
                    propertyId,
                    cancellationToken);

            ValidatePropertyGuestCapacity(
                property,
                request.GuestsCount);

            var hasConflictingBooking =
                await HasConflictingBookingAsync(
                    property.Id,
                    period.CheckInDate,
                    period.CheckOutDate,
                    cancellationToken);

            if (hasConflictingBooking)
            {
                throw new InvalidOperationException(
                    "The property is not available for the selected dates.");
            }

            var cancellationPolicy =
                GetPropertyCancellationPolicy(
                    property);

            var pricing =
                CalculatePricing(
                    property,
                    period.Nights);

            return new BookingQuoteResponse
            {
                PropertyId = property.Id,
                PropertyTitle = property.Title,
                CheckInDate = period.CheckInDate,
                CheckOutDate = period.CheckOutDate,
                GuestsCount = request.GuestsCount,
                Nights = period.Nights,
                PricePerNight = pricing.PricePerNight,
                Subtotal = pricing.Subtotal,
                ServiceFeePercentage =
                    ServiceFeeRate * 100,
                ServiceFee = pricing.ServiceFee,
                TotalAmount = pricing.TotalAmount,
                Currency = pricing.Currency,
                CancellationPolicy =
                    cancellationPolicy.ToString()
            };
        }

        /*
         * =====================================================
         * Create booking
         * =====================================================
         */

        public async Task<CreateBookingResponse>
            CreateAsync(
                Guid guestUserId,
                CreateBookingRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateGuestUserIdentifier(guestUserId);

            ArgumentNullException.ThrowIfNull(request);

            ValidateBookingTermsAcceptance(request);

            ValidatePropertyIdentifier(request.PropertyId);

            var period = ValidateBookingPeriod(
                request.CheckInDate,
                request.CheckOutDate);

            ValidateGuestsCount(request.GuestsCount);

            await EnsureActiveGuestExistsAsync(
                guestUserId,
                cancellationToken);

            await _userBookingRestrictionService
                .EnsureUserCanCreateBookingAsync(
                    guestUserId,
                    cancellationToken);

            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

            try
            {
                var property =
                    await GetBookablePropertyAsync(
                        request.PropertyId,
                        cancellationToken);

                if (property.HostProfile.UserId ==
                    guestUserId)
                {
                    throw new InvalidOperationException(
                        "You cannot book your own property.");
                }

                ValidatePropertyGuestCapacity(
                    property,
                    request.GuestsCount);

                /*
                 * Expired Pending bookings are ignored here,
                 * even if the lifecycle worker has not updated
                 * their database status yet.
                 */
                var hasConflictingBooking =
                    await HasConflictingBookingAsync(
                        property.Id,
                        period.CheckInDate,
                        period.CheckOutDate,
                        cancellationToken);

                if (hasConflictingBooking)
                {
                    throw new InvalidOperationException(
                        "The property is no longer available for the selected dates.");
                }

                var cancellationPolicy =
                    GetPropertyCancellationPolicy(
                        property);

                var pricing =
                    CalculatePricing(
                        property,
                        period.Nights);

                var currentTime =
                    DateTimeOffset.UtcNow;

                var expiresAt =
                    currentTime.AddMinutes(
                        PendingBookingExpirationMinutes);

                var booking = new Booking
                {
                    Id = Guid.NewGuid(),

                    PropertyId = property.Id,

                    GuestUserId = guestUserId,

                    CheckInDate = period.CheckInDate,

                    CheckOutDate = period.CheckOutDate,

                    GuestsCount = request.GuestsCount,

                    Nights = period.Nights,

                    PricePerNight = pricing.PricePerNight,

                    Subtotal = pricing.Subtotal,

                    ServiceFee = pricing.ServiceFee,

                    TotalAmount = pricing.TotalAmount,

                    Currency = pricing.Currency,

                    CancellationPolicySnapshot =
                        cancellationPolicy,

                    AcceptedBookingTerms =
                        request.AcceptedBookingTerms,

                    AcceptedCancellationPolicy =
                        request.AcceptedCancellationPolicy,

                    AcceptedPropertyRules =
                        request.AcceptedPropertyRules,

                    AcceptedComplaintPolicy =
                        request.AcceptedComplaintPolicy,

                    BookingTermsAcceptedAt =
                        currentTime,

                    Status = BookingStatus.Pending,

                    CancellationReason = null,

                    /*
                     * The dates remain reserved only until
                     * this payment deadline.
                     */
                    ExpiresAt = expiresAt,

                    CreatedAt = currentTime,

                    UpdatedAt = null,

                    ConfirmedAt = null,

                    CancelledAt = null,

                    ExpiredAt = null,

                    CompletedAt = null
                };

                await _dbContext.Bookings.AddAsync(
                    booking,
                    cancellationToken);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return new CreateBookingResponse
                {
                    BookingId = booking.Id,

                    PropertyId = property.Id,

                    PropertyTitle = property.Title,

                    GuestUserId = guestUserId,

                    CheckInDate = booking.CheckInDate,

                    CheckOutDate = booking.CheckOutDate,

                    GuestsCount = booking.GuestsCount,

                    Nights = booking.Nights,

                    PricePerNight = booking.PricePerNight,

                    Subtotal = booking.Subtotal,

                    ServiceFee = booking.ServiceFee,

                    TotalAmount = booking.TotalAmount,

                    Currency = booking.Currency,

                    CancellationPolicy =
                        booking
                            .CancellationPolicySnapshot
                            .ToString(),

                    Status = booking.Status.ToString(),

                    ExpiresAt = expiresAt,

                    CreatedAt = booking.CreatedAt,

                    Message =
                        "The booking was created successfully. " +
                        $"Complete payment before {expiresAt:O}."
                };
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }

        /*
         * =====================================================
         * Guest booking list
         * =====================================================
         */

        public async Task<GuestBookingsResponse>
            GetGuestBookingsAsync(
                Guid guestUserId,
                int page,
                int pageSize,
                BookingStatus? status,
                CancellationToken cancellationToken = default)
        {
            ValidateGuestUserIdentifier(guestUserId);

            ValidatePagination(
                page,
                pageSize);

            ValidateOptionalBookingStatus(
                status);

            await EnsureActiveGuestExistsAsync(
                guestUserId,
                cancellationToken);

            var query =
                _dbContext.Bookings
                    .AsNoTracking()
                    .Where(booking =>
                        booking.GuestUserId ==
                            guestUserId);

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
                await (
                    from booking in query

                    join review in
                        _dbContext.Reviews.AsNoTracking()
                    on booking.Id equals review.BookingId
                    into bookingReviews

                    from review in
                        bookingReviews.DefaultIfEmpty()

                    orderby booking.CreatedAt descending

                    select new
                    {
                        BookingId =
                            booking.Id,

                        booking.CheckInDate,
                        booking.CheckOutDate,
                        booking.Nights,
                        booking.GuestsCount,
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

                        ReviewId =
                            review == null
                                ? (Guid?)null
                                : review.Id,

                        ReviewStatus =
                            review == null
                                ? (ReviewStatus?)null
                                : review.Status
                    })
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(
                        cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            var today =
                GetUtcDate(currentTime);

            var items =
                rawItems
                    .Select(item =>
                        new GuestBookingListItemResponse
                        {
                            BookingId =
                                item.BookingId,

                            Property =
                                new GuestBookingPropertyResponse
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

                            CheckInDate =
                                item.CheckInDate,

                            CheckOutDate =
                                item.CheckOutDate,

                            Nights =
                                item.Nights,

                            GuestsCount =
                                item.GuestsCount,

                            TotalAmount =
                                item.TotalAmount,

                            Currency =
                                item.Currency,

                            Status =
                                item.Status.ToString(),

                            CanCancel =
                                CanCancelBooking(
                                    item.Status,
                                    item.CheckInDate,
                                    item.ExpiresAt,
                                    today,
                                    currentTime),

                            CanReview =
                                item.Status ==
                                    BookingStatus.Completed
                                &&
                                !item.ReviewId.HasValue,

                            HasReview =
                                item.ReviewId.HasValue,

                            ReviewId =
                                item.ReviewId,

                            ReviewStatus =
                                item.ReviewStatus
                                    .HasValue
                                        ? item.ReviewStatus
                                            .Value
                                            .ToString()
                                        : null,

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
                        })
                    .ToList();

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount /
                        (double)pageSize);

            return new GuestBookingsResponse
            {
                Items = items,

                Page = page,

                PageSize = pageSize,

                TotalCount = totalCount,

                TotalPages = totalPages,

                AppliedStatusFilter =
                    status?.ToString()
            };
        }

        /*
         * =====================================================
         * Guest booking details
         * =====================================================
         */

        public async Task<GuestBookingDetailsResponse>
            GetGuestBookingByIdAsync(
                Guid guestUserId,
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            ValidateGuestUserIdentifier(guestUserId);

            ValidateBookingIdentifier(bookingId);

            await EnsureActiveGuestExistsAsync(
                guestUserId,
                cancellationToken);

            var booking =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .Include(booking =>
                        booking.Property)
                    .ThenInclude(property =>
                        property.Images)
                    .SingleOrDefaultAsync(
                        booking =>
                            booking.Id == bookingId
                            &&
                            booking.GuestUserId ==
                                guestUserId,
                        cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    "The booking was not found.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            var today =
                GetUtcDate(currentTime);

            var canCancel =
                CanCancelBooking(
                    booking.Status,
                    booking.CheckInDate,
                    booking.ExpiresAt,
                    today,
                    currentTime);

            var refund =
                canCancel
                &&
                booking.Status ==
                    BookingStatus.Confirmed
                    ? CalculateEstimatedRefund(
                        booking,
                        today)
                    : RefundCalculationData.Empty;

            return new GuestBookingDetailsResponse
            {
                BookingId = booking.Id,

                Property =
                    MapGuestBookingProperty(
                        booking.Property),

                GuestUserId =
                    booking.GuestUserId,

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
                    booking.Currency,

                Status =
                    booking.Status.ToString(),

                CancellationPolicy =
                    GetBookingCancellationPolicy(
                        booking)
                    .ToString(),

                CanCancel =
                    canCancel,

                IsPaymentWindowExpired =
                    IsPaymentWindowExpired(
                        booking.Status,
                        booking.ExpiresAt,
                        currentTime),

                EstimatedRefundPercentage =
                    refund.Percentage,

                EstimatedRefundAmount =
                    refund.Amount,

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
         * Guest cancellation
         * =====================================================
         */

        public async Task<CancelBookingResponse>
            CancelGuestBookingAsync(
                Guid guestUserId,
                Guid bookingId,
                CancelBookingRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateGuestUserIdentifier(guestUserId);

            ValidateBookingIdentifier(bookingId);

            ArgumentNullException.ThrowIfNull(request);

            var normalizedReason =
                NormalizeCancellationReason(
                    request.Reason);

            await EnsureActiveGuestExistsAsync(
                guestUserId,
                cancellationToken);

            var booking =
                await _dbContext.Bookings
                    .SingleOrDefaultAsync(
                        booking =>
                            booking.Id == bookingId
                            &&
                            booking.GuestUserId ==
                                guestUserId,
                        cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    "The booking was not found.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            if (IsPaymentWindowExpired(
                    booking.Status,
                    booking.ExpiresAt,
                    currentTime))
            {
                booking.Status =
                    BookingStatus.Expired;

                booking.ExpiredAt =
                    currentTime;

                booking.UpdatedAt =
                    currentTime;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                throw new InvalidOperationException(
                    "The booking payment window has expired. " +
                    "The booking can no longer be cancelled.");
            }

            if (booking.Status !=
                    BookingStatus.Pending
                &&
                booking.Status !=
                    BookingStatus.Confirmed)
            {
                throw new InvalidOperationException(
                    $"Only pending or confirmed bookings can be cancelled. " +
                    $"The current booking status is '{booking.Status}'.");
            }

            var today =
                GetUtcDate(currentTime);

            if (booking.CheckInDate <= today)
            {
                throw new InvalidOperationException(
                    "The booking cannot be cancelled on or after the check-in date.");
            }

            var wasConfirmedBooking =
                booking.Status ==
                BookingStatus.Confirmed;

            var refund =
                wasConfirmedBooking
                    ? CalculateEstimatedRefund(
                        booking,
                        today)
                    : RefundCalculationData.Empty;

            var cancellationPolicy =
                GetBookingCancellationPolicy(
                    booking);

            booking.Status =
                BookingStatus.Cancelled;

            booking.CancellationReason =
                normalizedReason;

            booking.CancelledAt =
                currentTime;

            booking.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            PaymentRefundResponse? refundResponse =
                null;

            string? refundFailureMessage =
                null;

            /*
             * The cancellation has already been persisted.
             *
             * A Stripe or provider failure must therefore not make
             * the complete cancellation endpoint return an HTTP error.
             * The booking remains cancelled and the failed refund is
             * returned separately for retry or admin review.
             */
            if (wasConfirmedBooking
                &&
                refund.Amount > 0)
            {
                try
                {
                    refundResponse =
                        await _paymentRefundService
                            .CreateBookingCancellationRefundAsync(
                                guestUserId,
                                booking.Id,
                                refund.Amount,
                                cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    refundFailureMessage =
                        AutomaticRefundFailureMessage;

                    _logger.LogError(
                        exception,
                        "Booking {BookingId} was cancelled, but the refund " +
                        "of {RefundAmount} {Currency} could not be processed " +
                        "for guest {GuestUserId}.",
                        booking.Id,
                        refund.Amount,
                        booking.Currency,
                        guestUserId);
                }
            }

            await _userBookingRestrictionService
                .EvaluateGuestCancellationAsync(
                    guestUserId,
                    cancellationToken);

            return new CancelBookingResponse
            {
                BookingId =
                    booking.Id,

                Status =
                    booking.Status.ToString(),

                CancellationPolicy =
                    cancellationPolicy.ToString(),

                EstimatedRefundPercentage =
                    refund.Percentage,

                EstimatedRefundAmount =
                    refund.Amount,

                Currency =
                    booking.Currency,

                CancellationReason =
                    booking.CancellationReason,

                CancelledAt =
                    currentTime,

                IsRefundRequired =
                    wasConfirmedBooking
                    &&
                    refund.Amount > 0,

                RefundId =
                    refundResponse?.RefundId,

                ProviderRefundId =
                    refundResponse?.ProviderRefundId,

                RefundStatus =
                    refundResponse?.Status
                    ??
                    (refundFailureMessage is not null
                        ? nameof(PaymentRefundStatus.Failed)
                        : null),

                RefundAmount =
                    refundResponse?.Amount
                    ?? 0m,

                RefundMessage =
                    refundResponse?.Message
                    ?? refundFailureMessage,

                Message =
                    ResolveCancellationMessage(
                        wasConfirmedBooking,
                        refund.Amount,
                        refundResponse,
                        refundFailureMessage)
            };
        }

        /*
         * =====================================================
         * Database helpers
         * =====================================================
         */

        private async Task<Property>
            GetBookablePropertyAsync(
                Guid propertyId,
                CancellationToken cancellationToken)
        {
            var property =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.Status ==
                                PropertyStatus.Published
                            &&
                            property.HostProfile.Status ==
                                HostApplicationStatus.Approved
                            &&
                            property.HostProfile.User.IsActive,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found or is not available for booking.");
            }

            return property;
        }

        private async Task EnsureActiveGuestExistsAsync(
            Guid guestUserId,
            CancellationToken cancellationToken)
        {
            var guest =
                await _dbContext.Users
                    .AsNoTracking()
                    .Where(user =>
                        user.Id == guestUserId)
                    .Select(user =>
                        new
                        {
                            user.Id,
                            user.IsActive
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (guest is null)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user account was not found.");
            }

            if (!guest.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user account is inactive.");
            }
        }

        private async Task<bool>
            HasConflictingBookingAsync(
                Guid propertyId,
                DateOnly requestedCheckInDate,
                DateOnly requestedCheckOutDate,
                CancellationToken cancellationToken)
        {
            var currentTime =
                DateTimeOffset.UtcNow;

            return await _dbContext.Bookings
                .AsNoTracking()
                .AnyAsync(
                    booking =>
                        booking.PropertyId ==
                            propertyId
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
                        )
                        &&
                        booking.CheckInDate <
                            requestedCheckOutDate
                        &&
                        booking.CheckOutDate >
                            requestedCheckInDate,
                    cancellationToken);
        }

        /*
         * =====================================================
         * Booking lifecycle helpers
         * =====================================================
         */

        private static bool IsPaymentWindowExpired(
            BookingStatus status,
            DateTimeOffset? expiresAt,
            DateTimeOffset currentTime)
        {
            if (status == BookingStatus.Expired)
            {
                return true;
            }

            return status == BookingStatus.Pending
                   &&
                   (
                       !expiresAt.HasValue
                       ||
                       expiresAt.Value <= currentTime
                   );
        }

        private static bool CanCancelBooking(
            BookingStatus status,
            DateOnly checkInDate,
            DateTimeOffset? expiresAt,
            DateOnly today,
            DateTimeOffset currentTime)
        {
            if (checkInDate <= today)
            {
                return false;
            }

            return status switch
            {
                BookingStatus.Pending =>
                    expiresAt.HasValue
                    &&
                    expiresAt.Value > currentTime,

                BookingStatus.Confirmed =>
                    true,

                _ =>
                    false
            };
        }

        /*
         * =====================================================
         * Cancellation and refund helpers
         * =====================================================
         */

        private static string ResolveCancellationMessage(
            bool wasConfirmedBooking,
            decimal estimatedRefundAmount,
            PaymentRefundResponse? refundResponse,
            string? refundFailureMessage)
        {
            if (!wasConfirmedBooking)
            {
                return
                    "The pending booking was cancelled successfully. " +
                    "No refund is required because payment was not confirmed.";
            }

            if (estimatedRefundAmount <= 0)
            {
                return
                    "The booking was cancelled successfully. " +
                    "No refund is due according to the cancellation policy.";
            }

            if (!string.IsNullOrWhiteSpace(
                    refundFailureMessage))
            {
                return
                    "The booking was cancelled successfully, but the refund " +
                    "could not be processed automatically. It requires " +
                    "retry or admin review.";
            }

            if (refundResponse is null)
            {
                return
                    "The booking was cancelled successfully. " +
                    "The refund operation could not be created.";
            }

            return refundResponse.Status switch
            {
                nameof(PaymentRefundStatus.Succeeded) =>
                    "The booking was cancelled successfully and the refund was processed.",

                nameof(PaymentRefundStatus.Pending) =>
                    "The booking was cancelled successfully and the refund is pending provider processing.",

                nameof(PaymentRefundStatus.RequiresAction) =>
                    "The booking was cancelled successfully, but the refund requires additional provider action.",

                nameof(PaymentRefundStatus.Failed) =>
                    "The booking was cancelled successfully, but the refund failed.",

                nameof(PaymentRefundStatus.Cancelled) =>
                    "The booking was cancelled successfully, but the refund was cancelled by the provider.",

                _ =>
                    "The booking was cancelled successfully and the refund operation was created."
            };
        }

        private static RefundCalculationData
            CalculateEstimatedRefund(
                Booking booking,
                DateOnly today)
        {
            var cancellationPolicy =
                GetBookingCancellationPolicy(
                    booking);

            var daysBeforeCheckIn =
                booking.CheckInDate.DayNumber
                -
                today.DayNumber;

            decimal percentage =
                cancellationPolicy switch
                {
                    CancellationPolicyType.Flexible
                        when daysBeforeCheckIn >= 1 =>
                        100m,

                    CancellationPolicyType.Moderate
                        when daysBeforeCheckIn >= 5 =>
                        100m,

                    CancellationPolicyType.Moderate
                        when daysBeforeCheckIn >= 1 =>
                        50m,

                    CancellationPolicyType.Strict
                        when daysBeforeCheckIn >= 7 =>
                        50m,

                    _ =>
                        0m
                };

            var amount =
                RoundMoney(
                    booking.Subtotal
                    *
                    percentage
                    /
                    100m);

            return new RefundCalculationData
            {
                Percentage =
                    percentage,

                Amount =
                    amount
            };
        }

        private static string?
            NormalizeCancellationReason(
                string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return null;
            }

            var normalizedReason =
                reason.Trim();

            if (normalizedReason.Length >
                MaximumCancellationReasonLength)
            {
                throw new ArgumentException(
                    $"The cancellation reason cannot exceed " +
                    $"{MaximumCancellationReasonLength} characters.");
            }

            return normalizedReason;
        }

        /*
         * =====================================================
         * Property and pricing helpers
         * =====================================================
         */

        private static CancellationPolicyType
            GetPropertyCancellationPolicy(
                Property property)
        {
            if (!property.CancellationPolicy.HasValue
                ||
                !Enum.IsDefined(
                    property.CancellationPolicy.Value))
            {
                throw new InvalidOperationException(
                    "The property cancellation policy is not configured correctly.");
            }

            return property.CancellationPolicy.Value;
        }

        private static CancellationPolicyType
            GetBookingCancellationPolicy(
                Booking booking)
        {
            if (!Enum.IsDefined(
                    booking.CancellationPolicySnapshot))
            {
                throw new InvalidOperationException(
                    "The booking cancellation policy snapshot is invalid.");
            }

            return booking.CancellationPolicySnapshot;
        }

        private static void ValidatePropertyGuestCapacity(
            Property property,
            int guestsCount)
        {
            if (!property.MaxGuests.HasValue)
            {
                throw new InvalidOperationException(
                    "The property guest capacity is not configured.");
            }

            if (guestsCount >
                property.MaxGuests.Value)
            {
                throw new InvalidOperationException(
                    $"The property accommodates a maximum of " +
                    $"{property.MaxGuests.Value} guests.");
            }
        }

        private static BookingPricingData CalculatePricing(
            Property property,
            int nights)
        {
            if (!property.PricePerNight.HasValue
                ||
                property.PricePerNight.Value <= 0)
            {
                throw new InvalidOperationException(
                    "The property price is not configured correctly.");
            }

            var currency =
                CurrencyCodeNormalizer.NormalizeForStorage(
                    property.Currency);

            var pricePerNight =
                RoundMoney(
                    property.PricePerNight.Value);

            var subtotal =
                RoundMoney(
                    pricePerNight * nights);

            var serviceFee =
                RoundMoney(
                    subtotal * ServiceFeeRate);

            var totalAmount =
                RoundMoney(
                    subtotal + serviceFee);

            return new BookingPricingData
            {
                PricePerNight =
                    pricePerNight,

                Subtotal =
                    subtotal,

                ServiceFee =
                    serviceFee,

                TotalAmount =
                    totalAmount,

                Currency =
                    currency
            };
        }

        /*
         * =====================================================
         * Validation helpers
         * =====================================================
         */

        private static BookingPeriodData
            ValidateBookingPeriod(
                DateOnly? checkInDateValue,
                DateOnly? checkOutDateValue)
        {
            if (!checkInDateValue.HasValue)
            {
                throw new ArgumentException(
                    "The check-in date is required.");
            }

            if (!checkOutDateValue.HasValue)
            {
                throw new ArgumentException(
                    "The check-out date is required.");
            }

            var checkInDate =
                checkInDateValue.Value;

            var checkOutDate =
                checkOutDateValue.Value;

            var today =
                DateOnly.FromDateTime(
                    DateTime.UtcNow);

            if (checkInDate < today)
            {
                throw new ArgumentException(
                    "The check-in date cannot be in the past.");
            }

            if (checkOutDate <= checkInDate)
            {
                throw new ArgumentException(
                    "The check-out date must be after the check-in date.");
            }

            var nights =
                checkOutDate.DayNumber
                -
                checkInDate.DayNumber;

            if (nights <= 0)
            {
                throw new ArgumentException(
                    "The booking must contain at least one night.");
            }

            return new BookingPeriodData
            {
                CheckInDate =
                    checkInDate,

                CheckOutDate =
                    checkOutDate,

                Nights =
                    nights
            };
        }

        private static void ValidateGuestsCount(
            int guestsCount)
        {
            if (guestsCount is
                < MinimumGuestsCount
                or
                > MaximumGuestsCount)
            {
                throw new ArgumentException(
                    $"The guests count must be between " +
                    $"{MinimumGuestsCount} and " +
                    $"{MaximumGuestsCount}.");
            }
        }

        private static void ValidateBookingTermsAcceptance(
            CreateBookingRequest request)
        {
            if (!request.AcceptedBookingTerms
                ||
                !request.AcceptedCancellationPolicy
                ||
                !request.AcceptedPropertyRules
                ||
                !request.AcceptedComplaintPolicy)
            {
                throw new ArgumentException(
                    "You must accept the booking terms, cancellation policy, property rules, and complaint policy before creating a booking.");
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

        private static void ValidatePropertyIdentifier(
            Guid propertyId)
        {
            if (propertyId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The property identifier is invalid.");
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

        private static void ValidateGuestUserIdentifier(
            Guid guestUserId)
        {
            if (guestUserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain a valid user identifier.");
            }
        }

        /*
         * =====================================================
         * Mapping and general helpers
         * =====================================================
         */

        private static GuestBookingPropertyResponse
            MapGuestBookingProperty(
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

            return new GuestBookingPropertyResponse
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

        private static DateOnly GetUtcDate(
            DateTimeOffset dateTime)
        {
            return DateOnly.FromDateTime(
                dateTime.UtcDateTime);
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        /*
         * =====================================================
         * Internal models
         * =====================================================
         */

        private sealed class BookingPeriodData
        {
            public DateOnly CheckInDate { get; init; }

            public DateOnly CheckOutDate { get; init; }

            public int Nights { get; init; }
        }

        private sealed class BookingPricingData
        {
            public decimal PricePerNight { get; init; }

            public decimal Subtotal { get; init; }

            public decimal ServiceFee { get; init; }

            public decimal TotalAmount { get; init; }

            public string Currency { get; init; } =
                string.Empty;
        }

        private sealed class RefundCalculationData
        {
            public static RefundCalculationData Empty { get; } =
                new()
                {
                    Percentage = 0,
                    Amount = 0
                };

            public decimal Percentage { get; init; }

            public decimal Amount { get; init; }
        }
    }
}