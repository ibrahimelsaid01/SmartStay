using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartStayDAL;
using Stripe;

namespace SmartStayBLL
{
    public sealed class StripeWebhookService
        : IStripeWebhookService
    {
        private const string ProviderName =
            "STRIPE";

        private const string PaymentSucceededEvent =
            "payment_intent.succeeded";

        private const string PaymentFailedEvent =
            "payment_intent.payment_failed";

        private const string PaymentCanceledEvent =
            "payment_intent.canceled";

        private const string RefundCreatedEvent =
            "refund.created";

        private const string RefundUpdatedEvent =
            "refund.updated";

        private const string RefundFailedEvent =
            "refund.failed";

        private const int MaximumFailureReasonLength =
            100;

        private readonly SmartStayDbContext
            _dbContext;

        private readonly StripeSettings
            _stripeSettings;

        private readonly ILogger<StripeWebhookService>
            _logger;

        private readonly IBookingPayoutService
            _bookingPayoutService;

        public StripeWebhookService(
     SmartStayDbContext dbContext,
     IOptions<StripeSettings> stripeOptions,
     ILogger<StripeWebhookService> logger,
     IBookingPayoutService bookingPayoutService)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                stripeOptions);

            ArgumentNullException.ThrowIfNull(
                logger);

            ArgumentNullException.ThrowIfNull(
                bookingPayoutService);

            _dbContext =
                dbContext;

            _stripeSettings =
                stripeOptions.Value;

            _logger =
                logger;

            _bookingPayoutService =
                bookingPayoutService;
        }

        public async Task ProcessAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default)
        {
            var stripeEvent =
                ConstructStripeEvent(
                    payload,
                    signatureHeader);

            ValidateStripeEvent(
                stripeEvent);

            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

            var transactionCompleted =
                false;

            try
            {
                var wasAlreadyProcessed =
                    await _dbContext
                        .PaymentWebhookEvents
                        .AsNoTracking()
                        .AnyAsync(
                            webhookEvent =>
                                webhookEvent.Provider ==
                                    ProviderName
                                &&
                                webhookEvent.ProviderEventId ==
                                    stripeEvent.Id,
                            cancellationToken);

                if (wasAlreadyProcessed)
                {
                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted =
                        true;

                    _logger.LogInformation(
                        "Stripe event {EventId} was already processed.",
                        stripeEvent.Id);

                    return;
                }

                var receivedAt =
                    DateTimeOffset.UtcNow;

                var webhookEvent =
                    new PaymentWebhookEvent
                    {
                        Id =
                            Guid.NewGuid(),

                        Provider =
                            ProviderName,

                        ProviderEventId =
                            stripeEvent.Id,

                        EventType =
                            stripeEvent.Type,

                        ReceivedAt =
                            receivedAt,

                        ProcessedAt =
                            null
                    };

                await _dbContext
                    .PaymentWebhookEvents
                    .AddAsync(
                        webhookEvent,
                        cancellationToken);

                /*
                 * Save early so the unique index reserves
                 * this Stripe Event ID inside the current
                 * transaction.
                 */
                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await ProcessEventAsync(
                    stripeEvent,
                    cancellationToken);

                webhookEvent.ProcessedAt =
                    DateTimeOffset.UtcNow;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                transactionCompleted =
                    true;

                _logger.LogInformation(
                    "Stripe event {EventId} of type {EventType} " +
                    "was processed successfully.",
                    stripeEvent.Id,
                    stripeEvent.Type);
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

                var wasProcessedByAnotherRequest =
                    await _dbContext
                        .PaymentWebhookEvents
                        .AsNoTracking()
                        .AnyAsync(
                            webhookEvent =>
                                webhookEvent.Provider ==
                                    ProviderName
                                &&
                                webhookEvent.ProviderEventId ==
                                    stripeEvent.Id,
                            cancellationToken);

                if (wasProcessedByAnotherRequest)
                {
                    _logger.LogInformation(
                        "Stripe event {EventId} was processed " +
                        "by another concurrent request.",
                        stripeEvent.Id);

                    return;
                }

                throw;
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

        private async Task ProcessEventAsync(
            Event stripeEvent,
            CancellationToken cancellationToken)
        {
            switch (stripeEvent.Type)
            {
                case PaymentSucceededEvent:
                    {
                        var paymentIntent =
                            GetPaymentIntent(
                                stripeEvent);

                        await HandleSucceededAsync(
                            paymentIntent,
                            cancellationToken);

                        break;
                    }

                case PaymentFailedEvent:
                    {
                        var paymentIntent =
                            GetPaymentIntent(
                                stripeEvent);

                        await HandleFailedAsync(
                            paymentIntent,
                            cancellationToken);

                        break;
                    }

                case PaymentCanceledEvent:
                    {
                        var paymentIntent =
                            GetPaymentIntent(
                                stripeEvent);

                        await HandleCanceledAsync(
                            paymentIntent,
                            cancellationToken);

                        break;
                    }

                case RefundCreatedEvent:
                case RefundUpdatedEvent:
                case RefundFailedEvent:
                    {
                        var refund =
                            GetRefund(
                                stripeEvent);

                        await HandleRefundAsync(
                            refund,
                            cancellationToken);

                        break;
                    }

                default:
                    {
                        _logger.LogInformation(
                            "Stripe event type {EventType} is not " +
                            "handled by SmartStay.",
                            stripeEvent.Type);

                        break;
                    }
            }
        }

        /*
         * =====================================================
         * Succeeded payment
         * =====================================================
         */

        private async Task HandleSucceededAsync(
    PaymentIntent paymentIntent,
    CancellationToken cancellationToken)
        {
            var payment =
                await FindLocalPaymentAsync(
                    paymentIntent,
                    cancellationToken);

            if (payment is null)
            {
                return;
            }

            ValidatePaymentIntentFinancialValues(
                payment,
                paymentIntent,
                requireReceivedAmount:
                    true);

            if (payment.Status is
                PaymentStatus.Succeeded
                or PaymentStatus.PartiallyRefunded
                or PaymentStatus.Refunded)
            {
                if (payment.Status ==
                    PaymentStatus.Succeeded)
                {
                    await EnsureBookingPayoutExistsForSucceededPaymentAsync(
                        payment.Id,
                        cancellationToken);
                }

                return;
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            payment.Status =
                PaymentStatus.Succeeded;

            payment.SucceededAt =
                currentTime;

            payment.UpdatedAt =
                currentTime;

            payment.FailedAt =
                null;

            payment.CancelledAt =
                null;

            payment.FailureCode =
                null;

            payment.FailureMessage =
                null;

            if (!string.IsNullOrWhiteSpace(
                    paymentIntent.LatestChargeId))
            {
                payment.ProviderReference =
                    paymentIntent.LatestChargeId;
            }

            if (payment.Booking.Status ==
                    BookingStatus.Pending
                &&
                payment.Booking.ExpiresAt.HasValue
                &&
                payment.Booking.ExpiresAt.Value >
                    currentTime)
            {
                payment.Booking.Status =
                    BookingStatus.Confirmed;

                payment.Booking.ConfirmedAt ??=
                    currentTime;

                payment.Booking.UpdatedAt =
                    currentTime;

                await EnsureBookingPayoutExistsForSucceededPaymentAsync(
                    payment.Id,
                    cancellationToken);

                return;
            }

            if (payment.Booking.Status is
                BookingStatus.Confirmed
                or BookingStatus.Completed)
            {
                await EnsureBookingPayoutExistsForSucceededPaymentAsync(
                    payment.Id,
                    cancellationToken);

                return;
            }

            _logger.LogError(
                "Stripe payment {PaymentIntentId} succeeded, " +
                "but booking {BookingId} has status {BookingStatus}. " +
                "Manual reconciliation or refund may be required.",
                paymentIntent.Id,
                payment.BookingId,
                payment.Booking.Status);
        }

        /*
         * =====================================================
         * Failed payment
         * =====================================================
         */

        private async Task HandleFailedAsync(
            PaymentIntent paymentIntent,
            CancellationToken cancellationToken)
        {
            var payment =
                await FindLocalPaymentAsync(
                    paymentIntent,
                    cancellationToken);

            if (payment is null)
            {
                return;
            }

            ValidatePaymentIntentFinancialValues(
                payment,
                paymentIntent,
                requireReceivedAmount:
                    false);

            if (payment.Status is
                PaymentStatus.Succeeded
                or PaymentStatus.PartiallyRefunded
                or PaymentStatus.Refunded)
            {
                _logger.LogWarning(
                    "Ignored failed event for financially " +
                    "successful payment {PaymentId}.",
                    payment.Id);

                return;
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            payment.Status =
                PaymentStatus.Failed;

            payment.FailedAt =
                currentTime;

            payment.CancelledAt =
                null;

            payment.UpdatedAt =
                currentTime;

            payment.FailureCode =
                Truncate(
                    paymentIntent.LastPaymentError?.Code
                    ??
                    "stripe_payment_failed",
                    100);

            payment.FailureMessage =
                Truncate(
                    paymentIntent.LastPaymentError?.Message
                    ??
                    "Stripe reported that the payment attempt failed.",
                    1000);

            if (!string.IsNullOrWhiteSpace(
                    paymentIntent.LatestChargeId))
            {
                payment.ProviderReference =
                    paymentIntent.LatestChargeId;
            }
        }
        private async Task EnsureBookingPayoutExistsForSucceededPaymentAsync(
    Guid bookingPaymentId,
    CancellationToken cancellationToken)
        {
            var payout =
                await _bookingPayoutService
                    .CreatePendingPayoutForSucceededPaymentAsync(
                        bookingPaymentId,
                        cancellationToken);

            _logger.LogInformation(
                "Booking payout {PayoutId} with status {PayoutStatus} " +
                "was ensured for booking payment {BookingPaymentId}.",
                payout.PayoutId,
                payout.Status,
                bookingPaymentId);
        }
        /*
         * =====================================================
         * Canceled payment
         * =====================================================
         */

        private async Task HandleCanceledAsync(
            PaymentIntent paymentIntent,
            CancellationToken cancellationToken)
        {
            var payment =
                await FindLocalPaymentAsync(
                    paymentIntent,
                    cancellationToken);

            if (payment is null)
            {
                return;
            }

            ValidatePaymentIntentFinancialValues(
                payment,
                paymentIntent,
                requireReceivedAmount:
                    false);

            if (payment.Status is
                PaymentStatus.Succeeded
                or PaymentStatus.PartiallyRefunded
                or PaymentStatus.Refunded)
            {
                _logger.LogWarning(
                    "Ignored canceled event for financially " +
                    "successful payment {PaymentId}.",
                    payment.Id);

                return;
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            payment.Status =
                PaymentStatus.Cancelled;

            payment.CancelledAt =
                currentTime;

            payment.FailedAt =
                null;

            payment.UpdatedAt =
                currentTime;

            payment.FailureCode =
                Truncate(
                    paymentIntent.CancellationReason
                    ??
                    "stripe_payment_canceled",
                    100);

            payment.FailureMessage =
                "Stripe reported that the PaymentIntent " +
                "was canceled.";
        }

        /*
         * =====================================================
         * Refund events
         * =====================================================
         */

        private async Task HandleRefundAsync(
            Refund stripeRefund,
            CancellationToken cancellationToken)
        {
            ValidateStripeRefundIdentity(
                stripeRefund);

            var localRefund =
                await GetOrCreateLocalRefundFromWebhookAsync(
                    stripeRefund,
                    cancellationToken);

            if (localRefund is null)
            {
                return;
            }

            ValidateRefundFinancialValues(
                localRefund,
                stripeRefund);

            await ApplyStripeRefundToLocalRecordAsync(
                localRefund,
                stripeRefund,
                cancellationToken);
        }

        private async Task<BookingPaymentRefund?>
            GetOrCreateLocalRefundFromWebhookAsync(
                Refund stripeRefund,
                CancellationToken cancellationToken)
        {
            var existingRefund =
                await FindLocalRefundAsync(
                    stripeRefund,
                    cancellationToken);

            if (existingRefund is not null)
            {
                return existingRefund;
            }

            var payment =
                await FindLocalPaymentForRefundAsync(
                    stripeRefund,
                    cancellationToken);

            if (payment is null)
            {
                _logger.LogWarning(
                    "Stripe refund {RefundId} was ignored because " +
                    "SmartStay could not resolve its local payment.",
                    stripeRefund.Id);

                return null;
            }

            var normalizedCurrency =
                StripeAmountConverter.NormalizeCurrency(
                    stripeRefund.Currency);

            var amount =
                StripeAmountConverter.FromMinorUnit(
                    stripeRefund.Amount,
                    normalizedCurrency);

            var createdAt =
                ToUtcDateTimeOffset(
                    stripeRefund.Created);

            var localRefundId =
                GetLocalRefundIdentifier(
                    stripeRefund)
                ??
                Guid.NewGuid();

            var refund =
                new BookingPaymentRefund
                {
                    Id =
                        localRefundId,

                    BookingPaymentId =
                        payment.Id,

                    Amount =
                        amount,

                    Currency =
                        normalizedCurrency,

                    Provider =
                        ProviderName,

                    /*
                     * This path is mainly for manual Stripe
                     * refunds or webhooks arriving before the
                     * provider id has been saved locally.
                     */
                    IdempotencyKey =
                        $"stripe-refund:{stripeRefund.Id}",

                    ProviderRefundId =
                        stripeRefund.Id,

                    Status =
                        PaymentRefundStatus.Pending,

                    FailureReason =
                        null,

                    CreatedAt =
                        createdAt,

                    UpdatedAt =
                        null,

                    SucceededAt =
                        null,

                    FailedAt =
                        null,

                    CancelledAt =
                        null,

                    BookingPayment =
                        payment
                };

            await _dbContext.BookingPaymentRefunds
                .AddAsync(
                    refund,
                    cancellationToken);

            return refund;
        }

        private async Task<BookingPaymentRefund?>
            FindLocalRefundAsync(
                Refund stripeRefund,
                CancellationToken cancellationToken)
        {
            var localRefundId =
                GetLocalRefundIdentifier(
                    stripeRefund);

            if (localRefundId.HasValue)
            {
                var refundByLocalId =
                    await _dbContext.BookingPaymentRefunds
                        .Include(refund =>
                            refund.BookingPayment)
                        .ThenInclude(payment =>
                            payment.Booking)
                        .SingleOrDefaultAsync(
                            refund =>
                                refund.Id ==
                                    localRefundId.Value,
                            cancellationToken);

                if (refundByLocalId is not null)
                {
                    return refundByLocalId;
                }
            }

            return await _dbContext.BookingPaymentRefunds
                .Include(refund =>
                    refund.BookingPayment)
                .ThenInclude(payment =>
                    payment.Booking)
                .SingleOrDefaultAsync(
                    refund =>
                        refund.Provider ==
                            ProviderName
                        &&
                        refund.ProviderRefundId ==
                            stripeRefund.Id,
                    cancellationToken);
        }

        private async Task<BookingPayment?>
            FindLocalPaymentForRefundAsync(
                Refund stripeRefund,
                CancellationToken cancellationToken)
        {
            var localPaymentId =
                GetLocalPaymentIdentifier(
                    stripeRefund);

            if (localPaymentId.HasValue)
            {
                return await _dbContext.BookingPayments
                    .Include(payment =>
                        payment.Booking)
                    .SingleOrDefaultAsync(
                        payment =>
                            payment.Id ==
                                localPaymentId.Value,
                        cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(
                    stripeRefund.PaymentIntentId))
            {
                return null;
            }

            return await _dbContext.BookingPayments
                .Include(payment =>
                    payment.Booking)
                .SingleOrDefaultAsync(
                    payment =>
                        payment.Provider ==
                            ProviderName
                        &&
                        payment.ProviderPaymentId ==
                            stripeRefund.PaymentIntentId,
                    cancellationToken);
        }

        private async Task
            ApplyStripeRefundToLocalRecordAsync(
                BookingPaymentRefund localRefund,
                Refund stripeRefund,
                CancellationToken cancellationToken)
        {
            var currentTime =
                DateTimeOffset.UtcNow;

            var normalizedStatus =
                NormalizeStripeRefundStatus(
                    stripeRefund.Status);

            /*
             * Do not downgrade a successful refund because
             * Stripe events can arrive out of order.
             */
            if (localRefund.Status ==
                    PaymentRefundStatus.Succeeded
                &&
                normalizedStatus !=
                    "succeeded")
            {
                _logger.LogWarning(
                    "Ignored refund event with status {RefundStatus} " +
                    "for already succeeded refund {RefundId}.",
                    stripeRefund.Status,
                    localRefund.Id);

                return;
            }

            localRefund.ProviderRefundId =
                stripeRefund.Id;

            localRefund.UpdatedAt =
                EnsureNotBeforeCreatedAt(
                    localRefund.CreatedAt,
                    currentTime);

            switch (normalizedStatus)
            {
                case "pending":
                    if (IsTerminalRefundStatus(
                            localRefund.Status))
                    {
                        return;
                    }

                    localRefund.Status =
                        PaymentRefundStatus.Pending;

                    localRefund.FailureReason =
                        null;

                    localRefund.SucceededAt =
                        null;

                    localRefund.FailedAt =
                        null;

                    localRefund.CancelledAt =
                        null;
                    break;

                case "requires_action":
                    if (IsTerminalRefundStatus(
                            localRefund.Status))
                    {
                        return;
                    }

                    localRefund.Status =
                        PaymentRefundStatus
                            .RequiresAction;

                    localRefund.FailureReason =
                        null;

                    localRefund.SucceededAt =
                        null;

                    localRefund.FailedAt =
                        null;

                    localRefund.CancelledAt =
                        null;
                    break;

                case "succeeded":
                    localRefund.Status =
                        PaymentRefundStatus.Succeeded;

                    localRefund.FailureReason =
                        null;

                    localRefund.SucceededAt =
                        ResolveTerminalTimestamp(
                            localRefund.CreatedAt,
                            ToUtcDateTimeOffset(
                                stripeRefund.Created),
                            currentTime);

                    localRefund.FailedAt =
                        null;

                    localRefund.CancelledAt =
                        null;

                    await ApplySuccessfulRefundToPaymentAsync(
                        localRefund,
                        currentTime,
                        cancellationToken);
                    break;

                case "failed":
                    if (localRefund.Status ==
                        PaymentRefundStatus.Succeeded)
                    {
                        return;
                    }

                    localRefund.Status =
                        PaymentRefundStatus.Failed;

                    localRefund.FailureReason =
                        Truncate(
                            stripeRefund.FailureReason
                            ??
                            "Stripe reported that the refund failed.",
                            MaximumFailureReasonLength);

                    localRefund.FailedAt =
                        ResolveTerminalTimestamp(
                            localRefund.CreatedAt,
                            ToUtcDateTimeOffset(
                                stripeRefund.Created),
                            currentTime);

                    localRefund.SucceededAt =
                        null;

                    localRefund.CancelledAt =
                        null;
                    break;

                case "canceled":
                    if (localRefund.Status ==
                        PaymentRefundStatus.Succeeded)
                    {
                        return;
                    }

                    localRefund.Status =
                        PaymentRefundStatus.Cancelled;

                    localRefund.FailureReason =
                        Truncate(
                            stripeRefund.FailureReason
                            ??
                            "Stripe reported that the refund was canceled.",
                            MaximumFailureReasonLength);

                    localRefund.CancelledAt =
                        ResolveTerminalTimestamp(
                            localRefund.CreatedAt,
                            ToUtcDateTimeOffset(
                                stripeRefund.Created),
                            currentTime);

                    localRefund.SucceededAt =
                        null;

                    localRefund.FailedAt =
                        null;
                    break;

                default:
                    throw new PaymentProviderException(
                        $"Stripe returned unsupported refund status '{stripeRefund.Status}'.",
                        ProviderName);
            }
        }

        private async Task ApplySuccessfulRefundToPaymentAsync(
            BookingPaymentRefund currentRefund,
            DateTimeOffset currentTime,
            CancellationToken cancellationToken)
        {
            var payment =
                currentRefund.BookingPayment;

            if (!string.Equals(
                    payment.Provider,
                    ProviderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The refunded payment does not belong to Stripe.");
            }

            if (payment.Status is not
                PaymentStatus.Succeeded
                and not
                PaymentStatus.PartiallyRefunded
                and not
                PaymentStatus.Refunded)
            {
                throw new InvalidOperationException(
                    "The payment is not in a refundable status.");
            }

            var previousSuccessfulRefundTotal =
                await _dbContext.BookingPaymentRefunds
                    .Where(refund =>
                        refund.BookingPaymentId ==
                            payment.Id
                        &&
                        refund.Id !=
                            currentRefund.Id
                        &&
                        refund.Status ==
                            PaymentRefundStatus.Succeeded)
                    .Select(refund =>
                        refund.Amount)
                    .DefaultIfEmpty(0m)
                    .SumAsync(
                        cancellationToken);

            var totalRefundedAmount =
                RoundMoney(
                    previousSuccessfulRefundTotal
                    +
                    currentRefund.Amount);

            if (totalRefundedAmount >
                payment.Amount)
            {
                throw new InvalidOperationException(
                    "The total refunded amount cannot exceed the original payment amount.");
            }

            payment.RefundedAmount =
                totalRefundedAmount;

            payment.RefundedAt =
                EnsureNotBeforeCreatedAt(
                    payment.CreatedAt,
                    currentTime);

            payment.UpdatedAt =
                EnsureNotBeforeCreatedAt(
                    payment.CreatedAt,
                    currentTime);

            payment.Status =
                totalRefundedAmount == payment.Amount
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;
        }

        private static void ValidateStripeRefundIdentity(
            Refund stripeRefund)
        {
            if (string.IsNullOrWhiteSpace(
                    stripeRefund.Id))
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe Refund identifier is missing.");
            }

            if (!stripeRefund.Id.StartsWith(
                    "re_",
                    StringComparison.Ordinal))
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe Refund identifier is invalid.");
            }

            if (stripeRefund.Amount <= 0)
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe Refund amount is invalid.");
            }

            if (string.IsNullOrWhiteSpace(
                    stripeRefund.Currency))
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe Refund currency is missing.");
            }
        }

        private static void ValidateRefundFinancialValues(
            BookingPaymentRefund localRefund,
            Refund stripeRefund)
        {
            if (!string.IsNullOrWhiteSpace(
                    localRefund.ProviderRefundId)
                &&
                !string.Equals(
                    localRefund.ProviderRefundId,
                    stripeRefund.Id,
                    StringComparison.Ordinal))
            {
                throw new PaymentProviderException(
                    "Stripe returned a refund identifier that does not match the local refund record.",
                    ProviderName);
            }

            var expectedCurrency =
                StripeAmountConverter.NormalizeCurrency(
                    localRefund.Currency);

            if (!string.Equals(
                    stripeRefund.Currency,
                    expectedCurrency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new PaymentProviderException(
                    "Stripe refund webhook returned an unexpected currency.",
                    ProviderName);
            }

            var expectedAmount =
                StripeAmountConverter.ToMinorUnit(
                    localRefund.Amount,
                    expectedCurrency);

            if (stripeRefund.Amount !=
                expectedAmount)
            {
                throw new PaymentProviderException(
                    "Stripe refund webhook returned an unexpected amount.",
                    ProviderName);
            }
        }

        private static Guid?
            GetLocalRefundIdentifier(
                Refund stripeRefund)
        {
            if (stripeRefund.Metadata is null
                ||
                !stripeRefund.Metadata.TryGetValue(
                    "refundId",
                    out var refundIdValue)
                ||
                !Guid.TryParse(
                    refundIdValue,
                    out var refundId)
                ||
                refundId == Guid.Empty)
            {
                return null;
            }

            return refundId;
        }

        private static Guid?
            GetLocalPaymentIdentifier(
                Refund stripeRefund)
        {
            if (stripeRefund.Metadata is null
                ||
                !stripeRefund.Metadata.TryGetValue(
                    "paymentId",
                    out var paymentIdValue)
                ||
                !Guid.TryParse(
                    paymentIdValue,
                    out var paymentId)
                ||
                paymentId == Guid.Empty)
            {
                return null;
            }

            return paymentId;
        }

        private static string NormalizeStripeRefundStatus(
            string? status)
        {
            return status?
                .Trim()
                .ToLowerInvariant()
                ??
                string.Empty;
        }

        private static bool IsTerminalRefundStatus(
            PaymentRefundStatus status)
        {
            return status is
                PaymentRefundStatus.Succeeded
                or
                PaymentRefundStatus.Failed
                or
                PaymentRefundStatus.Cancelled;
        }

        /*
         * =====================================================
         * Local payment lookup
         * =====================================================
         */

        private async Task<BookingPayment?>
            FindLocalPaymentAsync(
                PaymentIntent paymentIntent,
                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                    paymentIntent.Id))
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe PaymentIntent identifier is missing.");
            }

            var localPaymentId =
                GetLocalPaymentIdentifier(
                    paymentIntent);

            if (!localPaymentId.HasValue)
            {
                _logger.LogWarning(
                    "Stripe PaymentIntent {PaymentIntentId} " +
                    "does not contain SmartStay payment metadata.",
                    paymentIntent.Id);

                return null;
            }

            var payment =
                await _dbContext.BookingPayments
                    .Include(payment =>
                        payment.Booking)
                    .SingleOrDefaultAsync(
                        payment =>
                            payment.Id ==
                                localPaymentId.Value,
                        cancellationToken);

            if (payment is null)
            {
                throw new KeyNotFoundException(
                    "The local payment referenced by the " +
                    "Stripe event was not found.");
            }

            if (!string.Equals(
                    payment.Provider,
                    ProviderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The local payment does not belong to Stripe.");
            }

            if (string.IsNullOrWhiteSpace(
                    payment.ProviderPaymentId))
            {
                payment.ProviderPaymentId =
                    paymentIntent.Id;
            }
            else if (!string.Equals(
                         payment.ProviderPaymentId,
                         paymentIntent.Id,
                         StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The Stripe PaymentIntent does not match " +
                    "the local payment.");
            }

            ValidateBookingMetadata(
                payment,
                paymentIntent);

            return payment;
        }

        private static Guid?
            GetLocalPaymentIdentifier(
                PaymentIntent paymentIntent)
        {
            if (paymentIntent.Metadata is null
                ||
                !paymentIntent.Metadata.TryGetValue(
                    "paymentId",
                    out var paymentIdValue)
                ||
                !Guid.TryParse(
                    paymentIdValue,
                    out var paymentId)
                ||
                paymentId == Guid.Empty)
            {
                return null;
            }

            return paymentId;
        }

        private static void ValidateBookingMetadata(
            BookingPayment payment,
            PaymentIntent paymentIntent)
        {
            if (paymentIntent.Metadata is null
                ||
                !paymentIntent.Metadata.TryGetValue(
                    "bookingId",
                    out var bookingIdValue))
            {
                return;
            }

            if (!Guid.TryParse(
                    bookingIdValue,
                    out var bookingId)
                ||
                bookingId !=
                    payment.BookingId)
            {
                throw new InvalidOperationException(
                    "The booking metadata in Stripe does not " +
                    "match the local payment.");
            }
        }

        /*
         * =====================================================
         * Financial validation
         * =====================================================
         */

        private static void
            ValidatePaymentIntentFinancialValues(
                BookingPayment payment,
                PaymentIntent paymentIntent,
                bool requireReceivedAmount)
        {
            var expectedCurrency =
                StripeAmountConverter.NormalizeCurrency(
                    payment.Currency);

            var expectedAmount =
                StripeAmountConverter.ToMinorUnit(
                    payment.Amount,
                    expectedCurrency);

            if (!string.Equals(
                    paymentIntent.Currency,
                    expectedCurrency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new PaymentProviderException(
                    "Stripe webhook returned an unexpected currency.",
                    ProviderName);
            }

            if (paymentIntent.Amount !=
                expectedAmount)
            {
                throw new PaymentProviderException(
                    "Stripe webhook returned an unexpected amount.",
                    ProviderName);
            }

            if (requireReceivedAmount
                &&
                paymentIntent.AmountReceived !=
                    expectedAmount)
            {
                throw new PaymentProviderException(
                    "Stripe reported success without receiving " +
                    "the expected amount.",
                    ProviderName);
            }
        }

        /*
         * =====================================================
         * Stripe signature validation
         * =====================================================
         */

        private Event ConstructStripeEvent(
            string payload,
            string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(
                    payload))
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe webhook payload is empty.");
            }

            if (string.IsNullOrWhiteSpace(
                    signatureHeader))
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe-Signature header is missing.");
            }

            if (string.IsNullOrWhiteSpace(
                    _stripeSettings.WebhookSecret))
            {
                throw new InvalidOperationException(
                    "Stripe webhook secret is not configured.");
            }

            try
            {
                return EventUtility.ConstructEvent(
                    payload,
                    signatureHeader,
                    _stripeSettings.WebhookSecret);
            }
            catch (StripeException exception)
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe webhook signature or payload is invalid.",
                    exception);
            }
        }

        private static void ValidateStripeEvent(
            Event stripeEvent)
        {
            if (string.IsNullOrWhiteSpace(
                    stripeEvent.Id))
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe event identifier is missing.");
            }

            if (string.IsNullOrWhiteSpace(
                    stripeEvent.Type))
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe event type is missing.");
            }
        }

        private static PaymentIntent GetPaymentIntent(
            Event stripeEvent)
        {
            if (stripeEvent.Data?.Object
                is not PaymentIntent paymentIntent)
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe event does not contain a " +
                    "valid PaymentIntent.");
            }

            return paymentIntent;
        }

        private static Refund GetRefund(
            Event stripeEvent)
        {
            if (stripeEvent.Data?.Object
                is not Refund refund)
            {
                throw new InvalidPaymentWebhookException(
                    "The Stripe event does not contain a " +
                    "valid Refund.");
            }

            return refund;
        }

        /*
         * =====================================================
         * Helpers
         * =====================================================
         */

        private static DateTimeOffset
            ResolveTerminalTimestamp(
                DateTimeOffset localCreatedAt,
                DateTimeOffset providerCreatedAt,
                DateTimeOffset currentTime)
        {
            if (providerCreatedAt >= localCreatedAt)
            {
                return providerCreatedAt;
            }

            return EnsureNotBeforeCreatedAt(
                localCreatedAt,
                currentTime);
        }

        private static DateTimeOffset
            EnsureNotBeforeCreatedAt(
                DateTimeOffset createdAt,
                DateTimeOffset timestamp)
        {
            return timestamp >= createdAt
                ? timestamp
                : createdAt;
        }

        private static DateTimeOffset ToUtcDateTimeOffset(
            DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return new DateTimeOffset(
                    dateTime);
            }

            return new DateTimeOffset(
                DateTime.SpecifyKind(
                    dateTime,
                    DateTimeKind.Utc));
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static string? Truncate(
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

            return normalizedValue.Length <=
                    maximumLength
                ? normalizedValue
                : normalizedValue[..maximumLength];
        }

        private static bool
            IsUniqueConstraintViolation(
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