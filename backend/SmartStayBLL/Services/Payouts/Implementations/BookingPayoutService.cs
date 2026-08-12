using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class BookingPayoutService
        : IBookingPayoutService
    {
        private const int MaximumReasonLength =
            1000;

        private readonly SmartStayDbContext
            _dbContext;

        public BookingPayoutService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        public async Task<BookingPayoutResponse>
            CreatePendingPayoutForSucceededPaymentAsync(
                Guid bookingPaymentId,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                bookingPaymentId,
                "The booking payment identifier is invalid.");

            var payment =
                await _dbContext.BookingPayments
                    .Include(paymentItem =>
                        paymentItem.Booking)
                    .ThenInclude(booking =>
                        booking.Property)
                    .SingleOrDefaultAsync(
                        paymentItem =>
                            paymentItem.Id == bookingPaymentId,
                        cancellationToken);

            if (payment is null)
            {
                throw new KeyNotFoundException(
                    "The booking payment was not found.");
            }

            if (payment.Status !=
                PaymentStatus.Succeeded)
            {
                throw new InvalidOperationException(
                    "A payout can only be created for a succeeded payment.");
            }

            if (payment.Booking.Status is not
                BookingStatus.Confirmed
                and not BookingStatus.Completed)
            {
                throw new InvalidOperationException(
                    "A payout can only be created for a confirmed or completed booking.");
            }

            var existingPayout =
                await _dbContext.BookingPayouts
                    .SingleOrDefaultAsync(
                        payout =>
                            payout.BookingPaymentId == payment.Id
                            ||
                            payout.BookingId == payment.BookingId,
                        cancellationToken);

            if (existingPayout is not null)
            {
                await PromoteDuePendingPayoutAsync(
                    existingPayout,
                    cancellationToken);

                return MapToResponse(
                    existingPayout);
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            var payoutAmount =
                payment.Booking.Subtotal > 0
                    ? payment.Booking.Subtotal
                    : payment.Amount;

            payoutAmount =
                RoundMoney(payoutAmount);

            if (payoutAmount <= 0)
            {
                throw new InvalidOperationException(
                    "The payout amount must be greater than zero.");
            }

            var availableAt =
                CalculateDefaultAvailableAt(
                    payment.Booking);

            var payout =
                new BookingPayout
                {
                    Id =
                        Guid.NewGuid(),

                    BookingId =
                        payment.BookingId,

                    BookingPaymentId =
                        payment.Id,

                    HostProfileId =
                        payment.Booking
                            .Property
                            .HostProfileId,

                    Amount =
                        payoutAmount,

                    Currency =
                        NormalizeCurrency(
                            payment.Currency),

                    Status =
                        DetermineOpenPayoutStatus(
                            availableAt,
                            currentTime),

                    AvailableAt =
                        availableAt,

                    HeldAt =
                        null,

                    HoldReason =
                        null,

                    ReleasedAt =
                        null,

                    ReleaseNote =
                        null,

                    PaidAt =
                        null,

                    BlockedAt =
                        null,

                    BlockReason =
                        null,

                    RefundedAt =
                        null,

                    CreatedAt =
                        currentTime,

                    UpdatedAt =
                        null
                };

            await _dbContext.BookingPayouts
                .AddAsync(
                    payout,
                    cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(exception))
            {
                _dbContext.Entry(payout).State =
                    EntityState.Detached;

                var concurrentPayout =
                    await _dbContext.BookingPayouts
                        .SingleOrDefaultAsync(
                            item =>
                                item.BookingPaymentId == payment.Id
                                ||
                                item.BookingId == payment.BookingId,
                            cancellationToken);

                if (concurrentPayout is null)
                {
                    throw;
                }

                await PromoteDuePendingPayoutAsync(
                    concurrentPayout,
                    cancellationToken);

                return MapToResponse(
                    concurrentPayout);
            }

            return MapToResponse(
                payout);
        }

        public async Task<BookingPayoutResponse?>
            GetByBookingIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                bookingId,
                "The booking identifier is invalid.");

            var payout =
                await _dbContext.BookingPayouts
                    .SingleOrDefaultAsync(
                        item =>
                            item.BookingId == bookingId,
                        cancellationToken);

            if (payout is null)
            {
                return null;
            }

            await PromoteDuePendingPayoutAsync(
                payout,
                cancellationToken);

            return MapToResponse(
                payout);
        }

        public async Task<BookingPayoutResponse>
            HoldPayoutForBookingAsync(
                Guid bookingId,
                string reason,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                bookingId,
                "The booking identifier is invalid.");

            var normalizedReason =
                NormalizeRequiredText(
                    reason,
                    "The payout hold reason is required.",
                    MaximumReasonLength);

            var payout =
                await GetTrackedPayoutByBookingIdAsync(
                    bookingId,
                    cancellationToken);

            if (payout.Status is
                BookingPayoutStatus.Paid
                or BookingPayoutStatus.Refunded
                or BookingPayoutStatus.Blocked)
            {
                throw new InvalidOperationException(
                    "This payout cannot be held because it is already paid, refunded, or blocked.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            payout.Status =
                BookingPayoutStatus.Held;

            payout.HeldAt ??=
                currentTime;

            payout.HoldReason =
                normalizedReason;

            payout.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToResponse(
                payout);
        }

        public async Task<BookingPayoutResponse>
            ReleasePayoutForBookingAsync(
                Guid bookingId,
                string? releaseNote,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                bookingId,
                "The booking identifier is invalid.");

            var normalizedReleaseNote =
                NormalizeOptionalText(
                    releaseNote,
                    MaximumReasonLength);

            var payout =
                await GetTrackedPayoutByBookingIdAsync(
                    bookingId,
                    cancellationToken);

            if (payout.Status !=
                BookingPayoutStatus.Held)
            {
                throw new InvalidOperationException(
                    "Only a held payout can be released.");
            }

            if (!payout.AvailableAt.HasValue)
            {
                throw new InvalidOperationException(
                    "The payout cannot be released because its availability schedule is missing.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            payout.Status =
                DetermineOpenPayoutStatus(
                    payout.AvailableAt.Value,
                    currentTime);

            payout.ReleasedAt =
                currentTime;

            payout.ReleaseNote =
                normalizedReleaseNote;

            payout.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToResponse(
                payout);
        }

        public async Task<BookingPayoutResponse>
            BlockPayoutForBookingAsync(
                Guid bookingId,
                string reason,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                bookingId,
                "The booking identifier is invalid.");

            var normalizedReason =
                NormalizeRequiredText(
                    reason,
                    "The payout block reason is required.",
                    MaximumReasonLength);

            var payout =
                await GetTrackedPayoutByBookingIdAsync(
                    bookingId,
                    cancellationToken);

            if (payout.Status is
                BookingPayoutStatus.Paid
                or BookingPayoutStatus.Refunded)
            {
                throw new InvalidOperationException(
                    "This payout cannot be blocked because it is already paid or refunded.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            payout.Status =
                BookingPayoutStatus.Blocked;

            payout.BlockedAt ??=
                currentTime;

            payout.BlockReason =
                normalizedReason;

            payout.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToResponse(
                payout);
        }

        public async Task<BookingPayoutResponse>
            MarkPayoutRefundedForBookingAsync(
                Guid bookingId,
                string? refundNote,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                bookingId,
                "The booking identifier is invalid.");

            /*
             * BookingPayout currently does not contain
             * a dedicated RefundNote field.
             *
             * The note is validated here but should be
             * stored in AdminActionLogs by the caller.
             */
            _ =
                NormalizeOptionalText(
                    refundNote,
                    MaximumReasonLength);

            var payout =
                await _dbContext.BookingPayouts
                    .Include(item =>
                        item.BookingPayment)
                    .SingleOrDefaultAsync(
                        item =>
                            item.BookingId == bookingId,
                        cancellationToken);

            if (payout is null)
            {
                throw new KeyNotFoundException(
                    "The booking payout was not found.");
            }

            if (payout.Status ==
                BookingPayoutStatus.Paid)
            {
                throw new InvalidOperationException(
                    "This payout cannot be marked as refunded because it is already paid.");
            }

            if (payout.Status ==
                BookingPayoutStatus.Refunded)
            {
                return MapToResponse(
                    payout);
            }

            var payment =
                payout.BookingPayment;

            if (payment.Status !=
                PaymentStatus.Refunded
                ||
                !payment.RefundedAt.HasValue
                ||
                RoundMoney(payment.RefundedAmount) !=
                    RoundMoney(payment.Amount))
            {
                throw new InvalidOperationException(
                    "The payout cannot be marked as refunded until the related payment is fully refunded.");
            }

            var successfulRefundTotal =
                RoundMoney(
                    await _dbContext.BookingPaymentRefunds
                        .AsNoTracking()
                        .Where(item =>
                            item.BookingPaymentId == payment.Id
                            &&
                            item.Status ==
                                PaymentRefundStatus.Succeeded)
                        .Select(item =>
                            item.Amount)
                        .DefaultIfEmpty(0m)
                        .SumAsync(
                            cancellationToken));

            if (successfulRefundTotal !=
                RoundMoney(payment.Amount))
            {
                throw new InvalidOperationException(
                    "The successful provider refund records do not equal the full payment amount.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            payout.Status =
                BookingPayoutStatus.Refunded;

            payout.RefundedAt =
                payment.RefundedAt.Value;

            payout.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToResponse(
                payout);
        }

        public async Task<BookingPayoutResponse>
            ReconcilePartialRefundForBookingAsync(
                Guid bookingId,
                Guid paymentRefundId,
                string? reconciliationNote,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                bookingId,
                "The booking identifier is invalid.");

            ValidateIdentifier(
                paymentRefundId,
                "The payment refund identifier is invalid.");

            var normalizedReconciliationNote =
                NormalizeOptionalText(
                    reconciliationNote,
                    MaximumReasonLength);

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var transactionCompleted =
                false;

            try
            {
                var payout =
                    await _dbContext.BookingPayouts
                        .Include(item =>
                            item.Booking)
                        .Include(item =>
                            item.BookingPayment)
                        .SingleOrDefaultAsync(
                            item =>
                                item.BookingId == bookingId,
                            cancellationToken);

                if (payout is null)
                {
                    throw new KeyNotFoundException(
                        "The booking payout was not found.");
                }

                if (payout.Status is
                    BookingPayoutStatus.Paid
                    or BookingPayoutStatus.Refunded)
                {
                    throw new InvalidOperationException(
                        "A paid or fully refunded payout cannot be reconciled for a partial refund.");
                }

                var refund =
                    await _dbContext.BookingPaymentRefunds
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            item =>
                                item.Id == paymentRefundId
                                &&
                                item.BookingPaymentId ==
                                    payout.BookingPaymentId,
                            cancellationToken);

                if (refund is null)
                {
                    throw new KeyNotFoundException(
                        "The successful payment refund linked to this payout was not found.");
                }

                if (refund.Status !=
                    PaymentRefundStatus.Succeeded
                    ||
                    !refund.SucceededAt.HasValue)
                {
                    throw new InvalidOperationException(
                        "Only a successful provider refund can reconcile a payout.");
                }

                var payment =
                    payout.BookingPayment;

                if (payment.BookingId !=
                    bookingId)
                {
                    throw new InvalidOperationException(
                        "The payout payment does not belong to the referenced booking.");
                }

                if (payment.Status !=
                    PaymentStatus.PartiallyRefunded)
                {
                    throw new InvalidOperationException(
                        "Partial payout reconciliation requires the related payment to be partially refunded.");
                }

                var originalPaymentAmount =
                    RoundMoney(
                        payment.Amount);

                var totalRefundedAmount =
                    RoundMoney(
                        payment.RefundedAmount);

                if (originalPaymentAmount <= 0)
                {
                    throw new InvalidOperationException(
                        "The original booking payment amount is invalid.");
                }

                if (totalRefundedAmount <= 0
                    ||
                    totalRefundedAmount >=
                        originalPaymentAmount)
                {
                    throw new InvalidOperationException(
                        "The refunded amount must be greater than zero and less than the original payment amount for a partial reconciliation.");
                }

                var successfulRefundTotal =
                    RoundMoney(
                        await _dbContext.BookingPaymentRefunds
                            .AsNoTracking()
                            .Where(item =>
                                item.BookingPaymentId == payment.Id
                                &&
                                item.Status ==
                                    PaymentRefundStatus.Succeeded)
                            .Select(item =>
                                item.Amount)
                            .DefaultIfEmpty(0m)
                            .SumAsync(
                                cancellationToken));

                if (successfulRefundTotal !=
                    totalRefundedAmount)
                {
                    throw new InvalidOperationException(
                        "The successful refund records do not match the payment refunded amount.");
                }

                var paymentCurrency =
                    NormalizeCurrency(
                        payment.Currency);

                var refundCurrency =
                    NormalizeCurrency(
                        refund.Currency);

                var payoutCurrency =
                    NormalizeCurrency(
                        payout.Currency);

                if (!string.Equals(
                        paymentCurrency,
                        refundCurrency,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    !string.Equals(
                        paymentCurrency,
                        payoutCurrency,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The payment, refund, and payout currencies do not match.");
                }

                var originalHostPayoutAmount =
                    payout.Booking.Subtotal > 0
                        ? RoundMoney(
                            payout.Booking.Subtotal)
                        : originalPaymentAmount;

                if (originalHostPayoutAmount <= 0)
                {
                    throw new InvalidOperationException(
                        "The original host payout amount is invalid.");
                }

                var remainingPaymentAmount =
                    RoundMoney(
                        originalPaymentAmount
                        -
                        totalRefundedAmount);

                var adjustedHostPayoutAmount =
                    RoundMoney(
                        originalHostPayoutAmount
                        *
                        remainingPaymentAmount
                        /
                        originalPaymentAmount);

                if (adjustedHostPayoutAmount <= 0)
                {
                    throw new InvalidOperationException(
                        "The remaining host payout rounds to zero. Use the full refund flow instead.");
                }

                if (adjustedHostPayoutAmount >
                    originalHostPayoutAmount)
                {
                    throw new InvalidOperationException(
                        "The adjusted host payout cannot exceed the original host payout amount.");
                }

                var currentTime =
                    DateTimeOffset.UtcNow;

                var availableAt =
                    payout.AvailableAt
                    ??
                    CalculateDefaultAvailableAt(
                        payout.Booking);

                var openStatus =
                    DetermineOpenPayoutStatus(
                        availableAt,
                        currentTime);

                /*
                 * A repeated request after a successful
                 * reconciliation must not reduce the payout
                 * a second time.
                 *
                 * The expected amount is always calculated
                 * from the immutable Booking.Subtotal and
                 * the total refunded payment amount.
                 */
                var isAlreadyReconciled =
                    payout.Status is
                        BookingPayoutStatus.Pending
                        or BookingPayoutStatus.Available
                    &&
                    RoundMoney(payout.Amount) ==
                        adjustedHostPayoutAmount;

                if (payout.Status !=
                        BookingPayoutStatus.Blocked
                    &&
                    !isAlreadyReconciled)
                {
                    throw new InvalidOperationException(
                        "The payout must be blocked before a partial refund can be reconciled.");
                }

                if (isAlreadyReconciled)
                {
                    if (payout.Status !=
                            openStatus
                        ||
                        payout.AvailableAt !=
                            availableAt)
                    {
                        payout.Status =
                            openStatus;

                        payout.AvailableAt =
                            availableAt;

                        payout.UpdatedAt =
                            currentTime;

                        await _dbContext.SaveChangesAsync(
                            cancellationToken);
                    }

                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted =
                        true;

                    return MapToResponse(
                        payout);
                }

                payout.Amount =
                    adjustedHostPayoutAmount;

                payout.AvailableAt =
                    availableAt;

                payout.Status =
                    openStatus;

                payout.ReleasedAt =
                    currentTime;

                payout.ReleaseNote =
                    normalizedReconciliationNote
                    ??
                    $"Partial refund {paymentRefundId} was reconciled against the host payout.";

                payout.UpdatedAt =
                    currentTime;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                transactionCompleted =
                    true;

                return MapToResponse(
                    payout);
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

        private async Task<BookingPayout>
            GetTrackedPayoutByBookingIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken)
        {
            var payout =
                await _dbContext.BookingPayouts
                    .SingleOrDefaultAsync(
                        item =>
                            item.BookingId == bookingId,
                        cancellationToken);

            if (payout is null)
            {
                throw new KeyNotFoundException(
                    "The booking payout was not found.");
            }

            return payout;
        }

        private async Task PromoteDuePendingPayoutAsync(
            BookingPayout payout,
            CancellationToken cancellationToken)
        {
            if (payout.Status !=
                    BookingPayoutStatus.Pending
                ||
                !payout.AvailableAt.HasValue
                ||
                payout.AvailableAt.Value >
                    DateTimeOffset.UtcNow)
            {
                return;
            }

            payout.Status =
                BookingPayoutStatus.Available;

            payout.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        private static BookingPayoutResponse MapToResponse(
            BookingPayout payout)
        {
            return new BookingPayoutResponse
            {
                PayoutId =
                    payout.Id,

                BookingId =
                    payout.BookingId,

                BookingPaymentId =
                    payout.BookingPaymentId,

                HostProfileId =
                    payout.HostProfileId,

                Amount =
                    payout.Amount,

                Currency =
                    payout.Currency,

                Status =
                    payout.Status.ToString(),

                AvailableAt =
                    payout.AvailableAt,

                HeldAt =
                    payout.HeldAt,

                HoldReason =
                    payout.HoldReason,

                ReleasedAt =
                    payout.ReleasedAt,

                ReleaseNote =
                    payout.ReleaseNote,

                PaidAt =
                    payout.PaidAt,

                BlockedAt =
                    payout.BlockedAt,

                BlockReason =
                    payout.BlockReason,

                RefundedAt =
                    payout.RefundedAt,

                CreatedAt =
                    payout.CreatedAt,

                UpdatedAt =
                    payout.UpdatedAt
            };
        }

        private static DateTimeOffset CalculateDefaultAvailableAt(
            Booking booking)
        {
            /*
             * Business rule:
             *
             * The host payout becomes available
             * 24 hours after the guest check-in date
             * when no complaint or dispute blocks it.
             *
             * Booking currently stores DateOnly.
             * Until property time zones are added,
             * the date is treated as UTC.
             */
            var checkInDateTime =
                booking.CheckInDate.ToDateTime(
                    TimeOnly.MinValue);

            var checkInDateTimeUtc =
                DateTime.SpecifyKind(
                    checkInDateTime,
                    DateTimeKind.Utc);

            return new DateTimeOffset(
                    checkInDateTimeUtc)
                .AddHours(
                    24);
        }

        private static BookingPayoutStatus DetermineOpenPayoutStatus(
            DateTimeOffset availableAt,
            DateTimeOffset currentTime)
        {
            return availableAt <=
                    currentTime
                ? BookingPayoutStatus.Available
                : BookingPayoutStatus.Pending;
        }

        private static string NormalizeCurrency(
            string currency)
        {
            if (string.IsNullOrWhiteSpace(
                    currency))
            {
                return "EGP";
            }

            var normalizedCurrency =
                currency
                    .Trim()
                    .ToUpperInvariant();

            if (normalizedCurrency ==
                "EGY")
            {
                return "EGP";
            }

            if (normalizedCurrency.Length !=
                3)
            {
                throw new InvalidOperationException(
                    "The payout currency must be a valid 3-letter currency code.");
            }

            return normalizedCurrency;
        }

        private static void ValidateIdentifier(
            Guid value,
            string errorMessage)
        {
            if (value ==
                Guid.Empty)
            {
                throw new ArgumentException(
                    errorMessage);
            }
        }

        private static string NormalizeRequiredText(
            string? value,
            string errorMessage,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    errorMessage);
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

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

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