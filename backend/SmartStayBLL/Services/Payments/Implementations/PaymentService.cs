using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class PaymentService
        : IPaymentService
    {
        private const string StripeProvider =
            "STRIPE";

        private const int MinimumIdempotencyKeyLength =
            8;

        private const int MaximumIdempotencyKeyLength =
            100;

        private readonly SmartStayDbContext
            _dbContext;

        private readonly IStripePaymentGateway
            _stripePaymentGateway;

        public PaymentService(
            SmartStayDbContext dbContext,
            IStripePaymentGateway stripePaymentGateway)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                stripePaymentGateway);

            _dbContext =
                dbContext;

            _stripePaymentGateway =
                stripePaymentGateway;
        }

        // =====================================================
        // Start payment
        // =====================================================

        public async Task<StartPaymentResponse>
            StartPaymentAsync(
                Guid guestUserId,
                StartPaymentRequest request,
                string idempotencyKey,
                CancellationToken cancellationToken = default)
        {
            ValidateGuestUserIdentifier(
                guestUserId);

            ArgumentNullException.ThrowIfNull(
                request);

            ValidateBookingIdentifier(
                request.BookingId);

            var normalizedIdempotencyKey =
                NormalizeIdempotencyKey(
                    idempotencyKey);

            await EnsureActiveGuestExistsAsync(
                guestUserId,
                cancellationToken);

            /*
             * First create or retrieve the local payment.
             *
             * The database transaction finishes before
             * contacting Stripe.
             */
            var localPayment =
                await GetOrCreateLocalPaymentAsync(
                    guestUserId,
                    request.BookingId,
                    normalizedIdempotencyKey,
                    cancellationToken);

            /*
             * Retrieve the existing Stripe PaymentIntent
             * or create one using deterministic idempotency.
             */
            var stripePaymentIntent =
                await ResolveStripePaymentIntentAsync(
                    localPayment,
                    guestUserId,
                    cancellationToken);

            ValidateStripePaymentIntent(
                localPayment,
                stripePaymentIntent);

            await SaveProviderPaymentIdAsync(
                localPayment.PaymentId,
                stripePaymentIntent.PaymentIntentId,
                cancellationToken);

            return MapStartPaymentResponse(
                localPayment,
                stripePaymentIntent);
        }

        // =====================================================
        // Get local payment status
        // =====================================================

        public async Task<PaymentStatusResponse>
            GetPaymentStatusAsync(
                Guid guestUserId,
                Guid paymentId,
                CancellationToken cancellationToken = default)
        {
            ValidateGuestUserIdentifier(
                guestUserId);

            ValidatePaymentIdentifier(
                paymentId);

            await EnsureActiveGuestExistsAsync(
                guestUserId,
                cancellationToken);

            var payment =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .Where(payment =>
                        payment.Id == paymentId
                        &&
                        payment.Booking.GuestUserId ==
                            guestUserId)
                    .Select(payment =>
                        new PaymentStatusData
                        {
                            PaymentId =
                                payment.Id,

                            BookingId =
                                payment.BookingId,

                            BookingStatus =
                                payment.Booking.Status,

                            Amount =
                                payment.Amount,

                            RefundedAmount =
                                payment.RefundedAmount,

                            Currency =
                                payment.Currency,

                            Provider =
                                payment.Provider,

                            ProviderPaymentId =
                                payment.ProviderPaymentId,

                            ProviderReference =
                                payment.ProviderReference,

                            Status =
                                payment.Status,

                            FailureCode =
                                payment.FailureCode,

                            FailureMessage =
                                payment.FailureMessage,

                            BookingExpiresAt =
                                payment.Booking.ExpiresAt,

                            CreatedAt =
                                payment.CreatedAt,

                            UpdatedAt =
                                payment.UpdatedAt,

                            SucceededAt =
                                payment.SucceededAt,

                            FailedAt =
                                payment.FailedAt,

                            CancelledAt =
                                payment.CancelledAt,

                            RefundedAt =
                                payment.RefundedAt
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (payment is null)
            {
                throw new KeyNotFoundException(
                    "The payment was not found.");
            }

            return new PaymentStatusResponse
            {
                PaymentId =
                    payment.PaymentId,

                BookingId =
                    payment.BookingId,

                BookingStatus =
                    payment.BookingStatus.ToString(),

                Amount =
                    payment.Amount,

                RefundedAmount =
                    payment.RefundedAmount,

                Currency =
                    StripeAmountConverter.NormalizeCurrency(
                        payment.Currency),

                Provider =
                    payment.Provider,

                ProviderPaymentId =
                    payment.ProviderPaymentId,

                ProviderReference =
                    payment.ProviderReference,

                Status =
                    payment.Status.ToString(),

                FailureCode =
                    payment.FailureCode,

                FailureMessage =
                    payment.FailureMessage,

                BookingExpiresAt =
                    payment.BookingExpiresAt,

                CreatedAt =
                    payment.CreatedAt,

                UpdatedAt =
                    payment.UpdatedAt,

                SucceededAt =
                    payment.SucceededAt,

                FailedAt =
                    payment.FailedAt,

                CancelledAt =
                    payment.CancelledAt,

                RefundedAt =
                    payment.RefundedAt,

                IsFinal =
                    payment.Status !=
                        PaymentStatus.Pending
            };
        }

        // =====================================================
        // Local payment creation
        // =====================================================

        private async Task<LocalPaymentData>
            GetOrCreateLocalPaymentAsync(
                Guid guestUserId,
                Guid bookingId,
                string idempotencyKey,
                CancellationToken cancellationToken)
        {
            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

            var transactionCompleted =
                false;

            try
            {
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

                /*
                 * Update an expired Pending booking immediately
                 * instead of waiting for the background service.
                 */
                if (booking.Status ==
                        BookingStatus.Pending
                    &&
                    IsBookingPaymentWindowExpired(
                        booking.ExpiresAt,
                        currentTime))
                {
                    booking.Status =
                        BookingStatus.Expired;

                    booking.ExpiredAt ??=
                        currentTime;

                    booking.UpdatedAt =
                        currentTime;

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);

                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted =
                        true;

                    throw new InvalidOperationException(
                        "The booking payment window has expired.");
                }

                /*
                 * Same BookingId + IdempotencyKey returns
                 * the same local payment.
                 */
                var existingPayment =
                    await FindLocalPaymentAsync(
                        guestUserId,
                        bookingId,
                        idempotencyKey,
                        cancellationToken);

                if (existingPayment is not null)
                {
                    ValidateExistingPaymentProvider(
                        existingPayment.Provider);

                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted =
                        true;

                    existingPayment.WasAlreadyProcessed =
                        true;

                    return existingPayment;
                }

                ValidateBookingCanStartPayment(
                    booking);

                var successfulPaymentExists =
                    await _dbContext.BookingPayments
                        .AsNoTracking()
                        .AnyAsync(
                            payment =>
                                payment.BookingId ==
                                    booking.Id
                                &&
                                payment.SucceededAt.HasValue,
                            cancellationToken);

                if (successfulPaymentExists)
                {
                    throw new InvalidOperationException(
                        "This booking already has a successful payment.");
                }

                var pendingPaymentExists =
                    await _dbContext.BookingPayments
                        .AsNoTracking()
                        .AnyAsync(
                            payment =>
                                payment.BookingId ==
                                    booking.Id
                                &&
                                payment.Status ==
                                    PaymentStatus.Pending,
                            cancellationToken);

                if (pendingPaymentExists)
                {
                    throw new InvalidOperationException(
                        "An active pending payment attempt already " +
                        "exists for this booking.");
                }

                /*
                 * Financial values always come from the
                 * stored booking, never from the client.
                 */
                var amount =
                    RoundMoney(
                        booking.TotalAmount);

                if (amount <= 0)
                {
                    throw new InvalidOperationException(
                        "The booking total amount is invalid.");
                }

                var currency =
                    StripeAmountConverter.NormalizeCurrency(
                        booking.Currency);

                /*
                 * Validate that Stripe can represent the
                 * amount using the currency minor unit.
                 */
                _ =
                    StripeAmountConverter.ToMinorUnit(
                        amount,
                        currency);

                var payment =
                    new BookingPayment
                    {
                        Id =
                            Guid.NewGuid(),

                        BookingId =
                            booking.Id,

                        Amount =
                            amount,

                        Currency =
                            currency,

                        Provider =
                            StripeProvider,

                        IdempotencyKey =
                            idempotencyKey,

                        ProviderPaymentId =
                            null,

                        ProviderReference =
                            null,

                        Status =
                            PaymentStatus.Pending,

                        RefundedAmount =
                            0m,

                        FailureCode =
                            null,

                        FailureMessage =
                            null,

                        CreatedAt =
                            currentTime,

                        UpdatedAt =
                            null,

                        SucceededAt =
                            null,

                        FailedAt =
                            null,

                        CancelledAt =
                            null,

                        RefundedAt =
                            null
                    };

                await _dbContext.BookingPayments
                    .AddAsync(
                        payment,
                        cancellationToken);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                transactionCompleted =
                    true;

                return new LocalPaymentData
                {
                    PaymentId =
                        payment.Id,

                    BookingId =
                        payment.BookingId,

                    Amount =
                        payment.Amount,

                    Currency =
                        payment.Currency,

                    Provider =
                        payment.Provider,

                    ProviderPaymentId =
                        payment.ProviderPaymentId,

                    Status =
                        payment.Status,

                    CreatedAt =
                        payment.CreatedAt,

                    BookingExpiresAt =
                        booking.ExpiresAt,

                    WasAlreadyProcessed =
                        false
                };
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(
                    exception))
            {
                if (!transactionCompleted)
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);

                    transactionCompleted =
                        true;
                }

                _dbContext.ChangeTracker.Clear();

                /*
                 * A concurrent request may have inserted
                 * the same idempotent payment first.
                 */
                var existingPayment =
                    await FindLocalPaymentAsync(
                        guestUserId,
                        bookingId,
                        idempotencyKey,
                        cancellationToken);

                if (existingPayment is not null)
                {
                    ValidateExistingPaymentProvider(
                        existingPayment.Provider);

                    existingPayment.WasAlreadyProcessed =
                        true;

                    return existingPayment;
                }

                throw new InvalidOperationException(
                    "Another active payment attempt already " +
                    "exists for this booking.",
                    exception);
            }
            catch
            {
                if (!transactionCompleted)
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);
                }

                throw;
            }
        }

        private async Task<LocalPaymentData?>
            FindLocalPaymentAsync(
                Guid guestUserId,
                Guid bookingId,
                string idempotencyKey,
                CancellationToken cancellationToken)
        {
            return await _dbContext.BookingPayments
                .AsNoTracking()
                .Where(payment =>
                    payment.BookingId ==
                        bookingId
                    &&
                    payment.IdempotencyKey ==
                        idempotencyKey
                    &&
                    payment.Booking.GuestUserId ==
                        guestUserId)
                .Select(payment =>
                    new LocalPaymentData
                    {
                        PaymentId =
                            payment.Id,

                        BookingId =
                            payment.BookingId,

                        Amount =
                            payment.Amount,

                        Currency =
                            payment.Currency,

                        Provider =
                            payment.Provider,

                        ProviderPaymentId =
                            payment.ProviderPaymentId,

                        Status =
                            payment.Status,

                        CreatedAt =
                            payment.CreatedAt,

                        BookingExpiresAt =
                            payment.Booking.ExpiresAt
                    })
                .SingleOrDefaultAsync(
                    cancellationToken);
        }

        // =====================================================
        // Stripe PaymentIntent
        // =====================================================

        private async Task<StripePaymentIntentResult>
            ResolveStripePaymentIntentAsync(
                LocalPaymentData localPayment,
                Guid guestUserId,
                CancellationToken cancellationToken)
        {
            /*
             * If the Stripe PaymentIntent ID was already saved,
             * retrieve and reuse the same PaymentIntent.
             */
            if (!string.IsNullOrWhiteSpace(
                    localPayment.ProviderPaymentId))
            {
                return await _stripePaymentGateway
                    .GetPaymentIntentAsync(
                        localPayment.ProviderPaymentId,
                        cancellationToken);
            }

            if (localPayment.Status !=
                PaymentStatus.Pending)
            {
                throw new InvalidOperationException(
                    "A Stripe PaymentIntent cannot be created " +
                    "for this payment status.");
            }

            /*
             * Deterministic Stripe idempotency key.
             *
             * Repeating the request for the same local payment
             * cannot create another PaymentIntent.
             */
            var providerIdempotencyKey =
                $"smartstay-payment-{localPayment.PaymentId:N}";

            return await _stripePaymentGateway
                .CreatePaymentIntentAsync(
                    new CreateStripePaymentIntentRequest
                    {
                        PaymentId =
                            localPayment.PaymentId,

                        BookingId =
                            localPayment.BookingId,

                        GuestUserId =
                            guestUserId,

                        Amount =
                            localPayment.Amount,

                        Currency =
                            localPayment.Currency,

                        ProviderIdempotencyKey =
                            providerIdempotencyKey,

                        BookingExpiresAt =
                            localPayment.BookingExpiresAt
                    },
                    cancellationToken);
        }

        private static void ValidateStripePaymentIntent(
            LocalPaymentData localPayment,
            StripePaymentIntentResult stripePaymentIntent)
        {
            if (string.IsNullOrWhiteSpace(
                    stripePaymentIntent.PaymentIntentId))
            {
                throw new PaymentProviderException(
                    "Stripe returned an invalid PaymentIntent identifier.",
                    StripeProvider);
            }

            if (string.IsNullOrWhiteSpace(
                    stripePaymentIntent.ClientSecret))
            {
                throw new PaymentProviderException(
                    "Stripe returned an empty client secret.",
                    StripeProvider);
            }

            var expectedCurrency =
                StripeAmountConverter.NormalizeCurrency(
                    localPayment.Currency);

            var expectedAmountInMinorUnit =
                StripeAmountConverter.ToMinorUnit(
                    localPayment.Amount,
                    expectedCurrency);

            if (!string.Equals(
                    stripePaymentIntent.Currency,
                    expectedCurrency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new PaymentProviderException(
                    "Stripe returned an unexpected currency.",
                    StripeProvider);
            }

            if (stripePaymentIntent.AmountInMinorUnit !=
                expectedAmountInMinorUnit)
            {
                throw new PaymentProviderException(
                    "Stripe returned an unexpected payment amount.",
                    StripeProvider);
            }
        }

        private async Task SaveProviderPaymentIdAsync(
            Guid paymentId,
            string providerPaymentId,
            CancellationToken cancellationToken)
        {
            var payment =
                await _dbContext.BookingPayments
                    .SingleOrDefaultAsync(
                        payment =>
                            payment.Id == paymentId,
                        cancellationToken);

            if (payment is null)
            {
                throw new KeyNotFoundException(
                    "The local payment record was not found.");
            }

            if (!string.Equals(
                    payment.Provider,
                    StripeProvider,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The local payment is not associated with Stripe.");
            }

            if (!string.IsNullOrWhiteSpace(
                    payment.ProviderPaymentId)
                &&
                !string.Equals(
                    payment.ProviderPaymentId,
                    providerPaymentId,
                    StringComparison.Ordinal))
            {
                throw new PaymentProviderException(
                    "The local payment is already connected " +
                    "to another Stripe PaymentIntent.",
                    StripeProvider);
            }

            if (string.IsNullOrWhiteSpace(
                    payment.ProviderPaymentId))
            {
                payment.ProviderPaymentId =
                    providerPaymentId;

                payment.UpdatedAt =
                    DateTimeOffset.UtcNow;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
        }

        // =====================================================
        // User and booking validation
        // =====================================================

        private async Task EnsureActiveGuestExistsAsync(
            Guid guestUserId,
            CancellationToken cancellationToken)
        {
            var user =
                await _dbContext.Users
                    .AsNoTracking()
                    .Where(user =>
                        user.Id == guestUserId)
                    .Select(user =>
                        new
                        {
                            user.IsActive
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (user is null)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user account was not found.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user account is inactive.");
            }
        }

        private static void ValidateBookingCanStartPayment(
            Booking booking)
        {
            if (booking.Status ==
                BookingStatus.Pending)
            {
                return;
            }

            var message =
                booking.Status switch
                {
                    BookingStatus.Confirmed =>
                        "The booking is already confirmed.",

                    BookingStatus.Cancelled =>
                        "A cancelled booking cannot be paid.",

                    BookingStatus.Completed =>
                        "A completed booking cannot be paid.",

                    BookingStatus.Expired =>
                        "The booking payment window has expired.",

                    _ =>
                        $"A payment cannot be started while " +
                        $"the booking status is '{booking.Status}'."
                };

            throw new InvalidOperationException(
                message);
        }

        private static bool
            IsBookingPaymentWindowExpired(
                DateTimeOffset? expiresAt,
                DateTimeOffset currentTime)
        {
            return !expiresAt.HasValue
                   ||
                   expiresAt.Value <=
                       currentTime;
        }

        private static void ValidateExistingPaymentProvider(
            string existingProvider)
        {
            if (!string.Equals(
                    existingProvider,
                    StripeProvider,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The existing payment is not associated " +
                    "with the Stripe payment provider.");
            }
        }

        // =====================================================
        // Idempotency and identifiers
        // =====================================================

        private static string NormalizeIdempotencyKey(
            string? idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(
                    idempotencyKey))
            {
                throw new ArgumentException(
                    "The Idempotency-Key header is required.");
            }

            var normalizedKey =
                idempotencyKey.Trim();

            if (normalizedKey.Length <
                    MinimumIdempotencyKeyLength
                ||
                normalizedKey.Length >
                    MaximumIdempotencyKeyLength)
            {
                throw new ArgumentException(
                    $"The idempotency key length must be between " +
                    $"{MinimumIdempotencyKeyLength} and " +
                    $"{MaximumIdempotencyKeyLength} characters.");
            }

            if (normalizedKey.Any(
                    char.IsControl))
            {
                throw new ArgumentException(
                    "The idempotency key contains invalid characters.");
            }

            return normalizedKey;
        }

        private static void ValidateGuestUserIdentifier(
            Guid guestUserId)
        {
            if (guestUserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain " +
                    "a valid user identifier.");
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

        private static void ValidatePaymentIdentifier(
            Guid paymentId)
        {
            if (paymentId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The payment identifier is invalid.");
            }
        }

        // =====================================================
        // Mapping
        // =====================================================

        private static StartPaymentResponse
            MapStartPaymentResponse(
                LocalPaymentData localPayment,
                StripePaymentIntentResult stripePaymentIntent)
        {
            return new StartPaymentResponse
            {
                PaymentId =
                    localPayment.PaymentId,

                BookingId =
                    localPayment.BookingId,

                Amount =
                    localPayment.Amount,

                Currency =
                    localPayment.Currency,

                Provider =
                    localPayment.Provider,

                ProviderPaymentId =
                    stripePaymentIntent.PaymentIntentId,

                ClientSecret =
                    stripePaymentIntent.ClientSecret,

                Status =
                    localPayment.Status.ToString(),

                ProviderStatus =
                    stripePaymentIntent.Status,

                BookingExpiresAt =
                    localPayment.BookingExpiresAt,

                CreatedAt =
                    localPayment.CreatedAt,

                WasAlreadyProcessed =
                    localPayment.WasAlreadyProcessed,

                Message =
                    localPayment.WasAlreadyProcessed
                        ? "The existing payment attempt was returned."
                        : "The Stripe PaymentIntent was created successfully."
            };
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        // =====================================================
        // Internal projection models
        // =====================================================

        private sealed class LocalPaymentData
        {
            public Guid PaymentId { get; init; }

            public Guid BookingId { get; init; }

            public decimal Amount { get; init; }

            public string Currency { get; init; } =
                string.Empty;

            public string Provider { get; init; } =
                string.Empty;

            public string? ProviderPaymentId { get; init; }

            public PaymentStatus Status { get; init; }

            public DateTimeOffset CreatedAt { get; init; }

            public DateTimeOffset? BookingExpiresAt
            {
                get;
                init;
            }

            public bool WasAlreadyProcessed
            {
                get;
                set;
            }
        }

        private sealed class PaymentStatusData
        {
            public Guid PaymentId { get; init; }

            public Guid BookingId { get; init; }

            public BookingStatus BookingStatus { get; init; }

            public decimal Amount { get; init; }

            public decimal RefundedAmount { get; init; }

            public string Currency { get; init; } =
                string.Empty;

            public string Provider { get; init; } =
                string.Empty;

            public string? ProviderPaymentId { get; init; }

            public string? ProviderReference { get; init; }

            public PaymentStatus Status { get; init; }

            public string? FailureCode { get; init; }

            public string? FailureMessage { get; init; }

            public DateTimeOffset? BookingExpiresAt
            {
                get;
                init;
            }

            public DateTimeOffset CreatedAt { get; init; }

            public DateTimeOffset? UpdatedAt { get; init; }

            public DateTimeOffset? SucceededAt { get; init; }

            public DateTimeOffset? FailedAt { get; init; }

            public DateTimeOffset? CancelledAt { get; init; }

            public DateTimeOffset? RefundedAt { get; init; }
        }

        // =====================================================
        // Database errors
        // =====================================================

        private static bool IsUniqueConstraintViolation(
            DbUpdateException exception)
        {
            return exception.InnerException
                is SqlException
            {
                Number: 2601 or 2627
            };
        }
    }
}