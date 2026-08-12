using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class PaymentRefundService : IPaymentRefundService
    {
        private const string StripeProvider = "STRIPE";

        private const int MaximumFailureReasonLength = 100;

        private readonly SmartStayDbContext _dbContext;

        private readonly IStripePaymentGateway _stripePaymentGateway;

        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentRefundService(
            SmartStayDbContext dbContext,
            IStripePaymentGateway stripePaymentGateway,
            UserManager<ApplicationUser> userManager)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            ArgumentNullException.ThrowIfNull(stripePaymentGateway);

            ArgumentNullException.ThrowIfNull(userManager);

            _dbContext = dbContext;

            _stripePaymentGateway = stripePaymentGateway;

            _userManager = userManager;
        }

        // =====================================================
        // Public operations
        // =====================================================

        public async Task<PaymentRefundResponse>
            CreateBookingCancellationRefundAsync(
                Guid guestUserId,
                Guid bookingId,
                decimal refundAmount,
                CancellationToken cancellationToken = default)
        {
            ValidateGuestUserIdentifier(guestUserId);

            ValidateBookingIdentifier(bookingId);

            var normalizedAmount =
                NormalizeRefundAmount(refundAmount);

            var localRefund =
                await GetOrCreateCancellationRefundAsync(
                    guestUserId,
                    bookingId,
                    normalizedAmount,
                    cancellationToken);

            return await ProcessLocalRefundAsync(
                localRefund,
                cancellationToken);
        }

        public async Task<PaymentRefundResponse>
            CreateSupportTicketRefundAsync(
                Guid adminUserId,
                Guid supportTicketId,
                decimal refundAmount,
                CancellationToken cancellationToken = default)
        {
            ValidateAdminUserIdentifier(adminUserId);

            ValidateSupportTicketIdentifier(supportTicketId);

            var normalizedAmount =
                NormalizeRefundAmount(refundAmount);

            await EnsureActiveAdminUserAsync(
                adminUserId,
                cancellationToken);

            var localRefund =
                await GetOrCreateSupportTicketRefundAsync(
                    supportTicketId,
                    normalizedAmount,
                    cancellationToken);

            return await ProcessLocalRefundAsync(
                localRefund,
                cancellationToken);
        }

        private async Task<PaymentRefundResponse>
            ProcessLocalRefundAsync(
                LocalRefundData localRefund,
                CancellationToken cancellationToken)
        {
            if (IsTerminalRefundStatus(localRefund.Status))
            {
                return await GetRefundResponseAsync(
                    localRefund.RefundId,
                    wasAlreadyProcessed: true,
                    cancellationToken);
            }

            var stripeRefund =
                await ResolveStripeRefundAsync(
                    localRefund,
                    cancellationToken);

            return await ApplyStripeRefundResultAsync(
                localRefund.RefundId,
                stripeRefund,
                localRefund.WasAlreadyProcessed,
                cancellationToken);
        }

        // =====================================================
        // Cancellation refund creation
        // =====================================================

        private async Task<LocalRefundData>
            GetOrCreateCancellationRefundAsync(
                Guid guestUserId,
                Guid bookingId,
                decimal refundAmount,
                CancellationToken cancellationToken)
        {
            var idempotencyKey =
                BuildBookingCancellationRefundIdempotencyKey(
                    bookingId);

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var transactionCompleted = false;

            try
            {
                var payment =
                    await _dbContext.BookingPayments
                        .Include(item => item.Booking)
                        .SingleOrDefaultAsync(
                            item =>
                                item.BookingId == bookingId
                                &&
                                item.Booking.GuestUserId == guestUserId
                                &&
                                item.SucceededAt.HasValue,
                            cancellationToken);

                if (payment is null)
                {
                    throw new InvalidOperationException(
                        "The booking does not have a successful payment that can be refunded.");
                }

                ValidateRefundablePayment(payment);

                var existingRefund =
                    await FindLocalRefundAsync(
                        payment.Id,
                        idempotencyKey,
                        cancellationToken);

                if (existingRefund is not null)
                {
                    EnsureExistingRefundMatches(
                        existingRefund,
                        payment.Id,
                        bookingId,
                        refundAmount,
                        "booking cancellation");

                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted = true;

                    existingRefund.WasAlreadyProcessed = true;

                    return existingRefund;
                }

                ValidateAmountDoesNotExceedRemaining(
                    payment,
                    refundAmount);

                var localRefund =
                    await CreateLocalRefundAsync(
                        payment,
                        refundAmount,
                        idempotencyKey,
                        cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                transactionCompleted = true;

                return localRefund;
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(exception))
            {
                if (!transactionCompleted)
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);

                    transactionCompleted = true;
                }

                _dbContext.ChangeTracker.Clear();

                var existingRefund =
                    await FindLocalRefundByIdempotencyKeyAsync(
                        idempotencyKey,
                        cancellationToken);

                if (existingRefund is not null)
                {
                    EnsureExistingRefundMatches(
                        existingRefund,
                        null,
                        bookingId,
                        refundAmount,
                        "booking cancellation");

                    existingRefund.WasAlreadyProcessed = true;

                    return existingRefund;
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

        // =====================================================
        // Support ticket refund creation
        // =====================================================

        private async Task<LocalRefundData>
            GetOrCreateSupportTicketRefundAsync(
                Guid supportTicketId,
                decimal refundAmount,
                CancellationToken cancellationToken)
        {
            var idempotencyKey =
                BuildSupportTicketRefundIdempotencyKey(
                    supportTicketId);

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var transactionCompleted = false;

            try
            {
                var ticket =
                    await _dbContext.SupportTickets
                        .AsNoTracking()
                        .Where(item =>
                            item.Id == supportTicketId)
                        .Select(item =>
                            new SupportTicketRefundContext
                            {
                                BookingId =
                                    item.BookingId,

                                Status =
                                    item.Status,

                                DecisionStatus =
                                    item.DecisionStatus,

                                DecisionAction =
                                    item.DecisionAction,

                                DecidedAt =
                                    item.DecidedAt,

                                DecidedByAdminId =
                                    item.DecidedByAdminId
                            })
                        .SingleOrDefaultAsync(
                            cancellationToken);

                if (ticket is null)
                {
                    throw new KeyNotFoundException(
                        "The support ticket was not found.");
                }

                ValidateSupportTicketRefundDecision(ticket);

                var bookingId =
                    ticket.BookingId!.Value;

                var payout =
                    await _dbContext.BookingPayouts
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            item =>
                                item.BookingId == bookingId,
                            cancellationToken);

                if (payout is null)
                {
                    throw new InvalidOperationException(
                        "The booking does not have a payout record that can be protected during the refund.");
                }

                if (payout.Status !=
                    BookingPayoutStatus.Blocked)
                {
                    throw new InvalidOperationException(
                        "The booking payout must be blocked before a support ticket refund can be processed.");
                }

                var payment =
                    await _dbContext.BookingPayments
                        .Include(item => item.Booking)
                        .SingleOrDefaultAsync(
                            item =>
                                item.BookingId == bookingId
                                &&
                                item.SucceededAt.HasValue,
                            cancellationToken);

                if (payment is null)
                {
                    throw new InvalidOperationException(
                        "The booking does not have a successful payment that can be refunded.");
                }

                ValidateRefundablePayment(payment);

                var existingRefund =
                    await FindLocalRefundAsync(
                        payment.Id,
                        idempotencyKey,
                        cancellationToken);

                if (existingRefund is not null)
                {
                    EnsureExistingRefundMatches(
                        existingRefund,
                        payment.Id,
                        bookingId,
                        refundAmount,
                        "support ticket");

                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted = true;

                    existingRefund.WasAlreadyProcessed = true;

                    return existingRefund;
                }

                ValidateSupportTicketRefundAmount(
                    ticket.DecisionAction,
                    payment,
                    refundAmount);

                var localRefund =
                    await CreateLocalRefundAsync(
                        payment,
                        refundAmount,
                        idempotencyKey,
                        cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                transactionCompleted = true;

                return localRefund;
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(exception))
            {
                if (!transactionCompleted)
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);

                    transactionCompleted = true;
                }

                _dbContext.ChangeTracker.Clear();

                var expectedBookingId =
                    await _dbContext.SupportTickets
                        .AsNoTracking()
                        .Where(item =>
                            item.Id == supportTicketId)
                        .Select(item =>
                            item.BookingId)
                        .SingleOrDefaultAsync(
                            cancellationToken);

                if (!expectedBookingId.HasValue
                    ||
                    expectedBookingId.Value == Guid.Empty)
                {
                    throw;
                }

                var existingRefund =
                    await FindLocalRefundByIdempotencyKeyAsync(
                        idempotencyKey,
                        cancellationToken);

                if (existingRefund is not null)
                {
                    EnsureExistingRefundMatches(
                        existingRefund,
                        null,
                        expectedBookingId.Value,
                        refundAmount,
                        "support ticket");

                    existingRefund.WasAlreadyProcessed = true;

                    return existingRefund;
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

        private async Task<LocalRefundData>
            CreateLocalRefundAsync(
                BookingPayment payment,
                decimal refundAmount,
                string idempotencyKey,
                CancellationToken cancellationToken)
        {
            var currentTime =
                DateTimeOffset.UtcNow;

            var refund =
                new BookingPaymentRefund
                {
                    Id =
                        Guid.NewGuid(),

                    BookingPaymentId =
                        payment.Id,

                    Amount =
                        refundAmount,

                    Currency =
                        StripeAmountConverter.NormalizeCurrency(
                            payment.Currency),

                    Provider =
                        StripeProvider,

                    IdempotencyKey =
                        idempotencyKey,

                    ProviderRefundId =
                        null,

                    Status =
                        PaymentRefundStatus.Pending,

                    FailureReason =
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
                        null
                };

            await _dbContext.BookingPaymentRefunds
                .AddAsync(
                    refund,
                    cancellationToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new LocalRefundData
            {
                RefundId =
                    refund.Id,

                PaymentId =
                    payment.Id,

                BookingId =
                    payment.BookingId,

                GuestUserId =
                    payment.Booking.GuestUserId,

                Amount =
                    refund.Amount,

                Currency =
                    refund.Currency,

                Provider =
                    refund.Provider,

                ProviderPaymentId =
                    payment.ProviderPaymentId!,

                ProviderRefundId =
                    refund.ProviderRefundId,

                Status =
                    refund.Status,

                ProviderIdempotencyKey =
                    refund.IdempotencyKey,

                WasAlreadyProcessed =
                    false
            };
        }

        private async Task<LocalRefundData?>
            FindLocalRefundAsync(
                Guid paymentId,
                string idempotencyKey,
                CancellationToken cancellationToken)
        {
            return await BuildLocalRefundQuery()
                .SingleOrDefaultAsync(
                    refund =>
                        refund.PaymentId == paymentId
                        &&
                        refund.ProviderIdempotencyKey ==
                            idempotencyKey,
                    cancellationToken);
        }

        private async Task<LocalRefundData?>
            FindLocalRefundByIdempotencyKeyAsync(
                string idempotencyKey,
                CancellationToken cancellationToken)
        {
            return await BuildLocalRefundQuery()
                .SingleOrDefaultAsync(
                    refund =>
                        refund.ProviderIdempotencyKey ==
                            idempotencyKey,
                    cancellationToken);
        }

        private IQueryable<LocalRefundData>
            BuildLocalRefundQuery()
        {
            return _dbContext.BookingPaymentRefunds
                .AsNoTracking()
                .Select(refund =>
                    new LocalRefundData
                    {
                        RefundId =
                            refund.Id,

                        PaymentId =
                            refund.BookingPaymentId,

                        BookingId =
                            refund.BookingPayment.BookingId,

                        GuestUserId =
                            refund.BookingPayment.Booking.GuestUserId,

                        Amount =
                            refund.Amount,

                        Currency =
                            refund.Currency,

                        Provider =
                            refund.Provider,

                        ProviderPaymentId =
                            refund.BookingPayment.ProviderPaymentId!,

                        ProviderRefundId =
                            refund.ProviderRefundId,

                        Status =
                            refund.Status,

                        ProviderIdempotencyKey =
                            refund.IdempotencyKey
                    });
        }

        // =====================================================
        // Stripe interaction
        // =====================================================

        private async Task<StripeRefundResult>
            ResolveStripeRefundAsync(
                LocalRefundData localRefund,
                CancellationToken cancellationToken)
        {
            ValidateLocalRefundForProviderCall(localRefund);

            if (!string.IsNullOrWhiteSpace(
                    localRefund.ProviderRefundId))
            {
                return await _stripePaymentGateway
                    .GetRefundAsync(
                        localRefund.ProviderRefundId,
                        cancellationToken);
            }

            return await _stripePaymentGateway
                .CreateRefundAsync(
                    new CreateStripeRefundRequest
                    {
                        RefundId =
                            localRefund.RefundId,

                        PaymentId =
                            localRefund.PaymentId,

                        BookingId =
                            localRefund.BookingId,

                        GuestUserId =
                            localRefund.GuestUserId,

                        ProviderPaymentId =
                            localRefund.ProviderPaymentId,

                        Amount =
                            localRefund.Amount,

                        Currency =
                            localRefund.Currency,

                        ProviderIdempotencyKey =
                            localRefund.ProviderIdempotencyKey
                    },
                    cancellationToken);
        }

        // =====================================================
        // Apply Stripe result
        // =====================================================

        private async Task<PaymentRefundResponse>
            ApplyStripeRefundResultAsync(
                Guid refundId,
                StripeRefundResult stripeRefund,
                bool wasAlreadyProcessed,
                CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                stripeRefund);

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var transactionCompleted = false;

            try
            {
                var refund =
                    await _dbContext.BookingPaymentRefunds
                        .Include(item =>
                            item.BookingPayment)
                        .ThenInclude(payment =>
                            payment.Booking)
                        .SingleOrDefaultAsync(
                            item =>
                                item.Id == refundId,
                            cancellationToken);

                if (refund is null)
                {
                    throw new KeyNotFoundException(
                        "The refund record was not found.");
                }

                ValidateStripeRefundResult(
                    refund,
                    stripeRefund);

                var currentTime =
                    DateTimeOffset.UtcNow;

                refund.ProviderRefundId =
                    stripeRefund.RefundId;

                refund.UpdatedAt =
                    EnsureNotBeforeCreatedAt(
                        refund.CreatedAt,
                        currentTime);

                switch (NormalizeStripeRefundStatus(
                    stripeRefund.Status))
                {
                    case "pending":
                        SetPendingRefundState(refund);
                        break;

                    case "requires_action":
                        SetRequiresActionRefundState(refund);
                        break;

                    case "succeeded":
                        SetSucceededRefundState(
                            refund,
                            stripeRefund,
                            currentTime);

                        await ApplySuccessfulRefundToPaymentAsync(
                            refund,
                            currentTime,
                            cancellationToken);
                        break;

                    case "failed":
                        SetFailedRefundState(
                            refund,
                            stripeRefund,
                            currentTime);
                        break;

                    case "canceled":
                        SetCancelledRefundState(
                            refund,
                            stripeRefund,
                            currentTime);
                        break;

                    default:
                        throw new PaymentProviderException(
                            $"Stripe returned unsupported refund status '{stripeRefund.Status}'.",
                            StripeProvider);
                }

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                transactionCompleted = true;

                return MapResponse(
                    refund,
                    wasAlreadyProcessed);
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

        private static void SetPendingRefundState(
            BookingPaymentRefund refund)
        {
            refund.Status =
                PaymentRefundStatus.Pending;

            refund.FailureReason =
                null;

            refund.SucceededAt =
                null;

            refund.FailedAt =
                null;

            refund.CancelledAt =
                null;
        }

        private static void SetRequiresActionRefundState(
            BookingPaymentRefund refund)
        {
            refund.Status =
                PaymentRefundStatus.RequiresAction;

            refund.FailureReason =
                null;

            refund.SucceededAt =
                null;

            refund.FailedAt =
                null;

            refund.CancelledAt =
                null;
        }

        private static void SetSucceededRefundState(
            BookingPaymentRefund refund,
            StripeRefundResult stripeRefund,
            DateTimeOffset currentTime)
        {
            refund.Status =
                PaymentRefundStatus.Succeeded;

            refund.FailureReason =
                null;

            refund.SucceededAt =
                ResolveTerminalTimestamp(
                    refund.CreatedAt,
                    stripeRefund.CreatedAt,
                    currentTime);

            refund.FailedAt =
                null;

            refund.CancelledAt =
                null;
        }

        private static void SetFailedRefundState(
            BookingPaymentRefund refund,
            StripeRefundResult stripeRefund,
            DateTimeOffset currentTime)
        {
            refund.Status =
                PaymentRefundStatus.Failed;

            refund.FailureReason =
                NormalizeFailureReason(
                    stripeRefund.FailureReason
                    ??
                    "Stripe reported that the refund failed.");

            refund.FailedAt =
                ResolveTerminalTimestamp(
                    refund.CreatedAt,
                    stripeRefund.CreatedAt,
                    currentTime);

            refund.SucceededAt =
                null;

            refund.CancelledAt =
                null;
        }

        private static void SetCancelledRefundState(
            BookingPaymentRefund refund,
            StripeRefundResult stripeRefund,
            DateTimeOffset currentTime)
        {
            refund.Status =
                PaymentRefundStatus.Cancelled;

            refund.FailureReason =
                NormalizeFailureReason(
                    stripeRefund.FailureReason
                    ??
                    "Stripe reported that the refund was cancelled.");

            refund.CancelledAt =
                ResolveTerminalTimestamp(
                    refund.CreatedAt,
                    stripeRefund.CreatedAt,
                    currentTime);

            refund.SucceededAt =
                null;

            refund.FailedAt =
                null;
        }

        private async Task ApplySuccessfulRefundToPaymentAsync(
            BookingPaymentRefund currentRefund,
            DateTimeOffset currentTime,
            CancellationToken cancellationToken)
        {
            var payment =
                currentRefund.BookingPayment;

            if (payment.Status ==
                PaymentStatus.Refunded)
            {
                return;
            }

            if (payment.Status is not
                PaymentStatus.Succeeded
                and not
                PaymentStatus.PartiallyRefunded)
            {
                throw new InvalidOperationException(
                    "The payment is not in a refundable status.");
            }

            var previousSuccessfulRefundTotal =
                await _dbContext.BookingPaymentRefunds
                    .Where(refund =>
                        refund.BookingPaymentId == payment.Id
                        &&
                        refund.Id != currentRefund.Id
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

        // =====================================================
        // Response mapping
        // =====================================================

        private async Task<PaymentRefundResponse>
            GetRefundResponseAsync(
                Guid refundId,
                bool wasAlreadyProcessed,
                CancellationToken cancellationToken)
        {
            var refund =
                await _dbContext.BookingPaymentRefunds
                    .AsNoTracking()
                    .Include(item =>
                        item.BookingPayment)
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == refundId,
                        cancellationToken);

            if (refund is null)
            {
                throw new KeyNotFoundException(
                    "The refund record was not found.");
            }

            return MapResponse(
                refund,
                wasAlreadyProcessed);
        }

        private static PaymentRefundResponse MapResponse(
            BookingPaymentRefund refund,
            bool wasAlreadyProcessed)
        {
            return new PaymentRefundResponse
            {
                RefundId =
                    refund.Id,

                PaymentId =
                    refund.BookingPaymentId,

                BookingId =
                    refund.BookingPayment?.BookingId
                    ??
                    Guid.Empty,

                Amount =
                    refund.Amount,

                Currency =
                    refund.Currency,

                Provider =
                    refund.Provider,

                ProviderRefundId =
                    refund.ProviderRefundId,

                Status =
                    refund.Status.ToString(),

                FailureReason =
                    refund.FailureReason,

                CreatedAt =
                    refund.CreatedAt,

                UpdatedAt =
                    refund.UpdatedAt,

                SucceededAt =
                    refund.SucceededAt,

                FailedAt =
                    refund.FailedAt,

                CancelledAt =
                    refund.CancelledAt,

                WasAlreadyProcessed =
                    wasAlreadyProcessed,

                Message =
                    ResolveMessage(
                        refund.Status,
                        wasAlreadyProcessed)
            };
        }

        private static string ResolveMessage(
            PaymentRefundStatus status,
            bool wasAlreadyProcessed)
        {
            if (wasAlreadyProcessed)
            {
                return "The existing refund operation was returned.";
            }

            return status switch
            {
                PaymentRefundStatus.Pending =>
                    "The refund operation is pending provider processing.",

                PaymentRefundStatus.RequiresAction =>
                    "The refund requires additional provider action.",

                PaymentRefundStatus.Succeeded =>
                    "The refund was processed successfully.",

                PaymentRefundStatus.Failed =>
                    "The refund failed.",

                PaymentRefundStatus.Cancelled =>
                    "The refund was cancelled.",

                _ =>
                    "The refund status is unknown."
            };
        }

        // =====================================================
        // Authorization and business validation
        // =====================================================

        private async Task EnsureActiveAdminUserAsync(
            Guid adminUserId,
            CancellationToken cancellationToken)
        {
            var adminUser =
                await _dbContext.Users
                    .SingleOrDefaultAsync(
                        user =>
                            user.Id == adminUserId
                            &&
                            user.IsActive,
                        cancellationToken);

            if (adminUser is null)
            {
                throw new UnauthorizedAccessException(
                    "The admin user was not found or is inactive.");
            }

            var isAdmin =
                await _userManager.IsInRoleAsync(
                    adminUser,
                    RoleNames.Admin);

            if (!isAdmin)
            {
                throw new UnauthorizedAccessException(
                    "Only admins can process support ticket refunds.");
            }
        }

        private static void ValidateSupportTicketRefundDecision(
            SupportTicketRefundContext ticket)
        {
            if (!ticket.BookingId.HasValue
                ||
                ticket.BookingId.Value == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The support ticket is not linked to a valid booking.");
            }

            if (ticket.Status is
                SupportTicketStatus.Resolved
                or
                SupportTicketStatus.Closed)
            {
                throw new InvalidOperationException(
                    "A refund cannot be started from a resolved or closed support ticket.");
            }

            if (ticket.DecisionStatus !=
                SupportTicketDecisionStatus.ValidComplaint)
            {
                throw new InvalidOperationException(
                    "Only a valid complaint can create a support ticket refund.");
            }

            if (ticket.DecisionAction is not
                SupportTicketDecisionAction.PartialRefundRecommended
                and not
                SupportTicketDecisionAction.FullRefundRecommended)
            {
                throw new InvalidOperationException(
                    "The support ticket decision does not recommend a refund.");
            }

            if (!ticket.DecidedAt.HasValue
                ||
                !ticket.DecidedByAdminId.HasValue
                ||
                ticket.DecidedByAdminId.Value == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The support ticket does not contain a complete admin decision.");
            }
        }

        private static void ValidateSupportTicketRefundAmount(
            SupportTicketDecisionAction decisionAction,
            BookingPayment payment,
            decimal requestedRefundAmount)
        {
            var remainingRefundableAmount =
                RoundMoney(
                    payment.Amount
                    -
                    payment.RefundedAmount);

            if (remainingRefundableAmount <= 0)
            {
                throw new InvalidOperationException(
                    "The payment has no remaining refundable amount.");
            }

            if (requestedRefundAmount >
                remainingRefundableAmount)
            {
                throw new InvalidOperationException(
                    "The requested refund amount exceeds the remaining refundable payment amount.");
            }

            var resultingRefundedAmount =
                RoundMoney(
                    payment.RefundedAmount
                    +
                    requestedRefundAmount);

            if (decisionAction ==
                SupportTicketDecisionAction.FullRefundRecommended
                &&
                resultingRefundedAmount != payment.Amount)
            {
                throw new InvalidOperationException(
                    "A full refund decision must refund the entire remaining payment amount.");
            }

            if (decisionAction ==
                SupportTicketDecisionAction.PartialRefundRecommended
                &&
                resultingRefundedAmount >= payment.Amount)
            {
                throw new InvalidOperationException(
                    "A partial refund decision must leave part of the original payment unrefunded.");
            }
        }

        private static void ValidateAmountDoesNotExceedRemaining(
            BookingPayment payment,
            decimal requestedRefundAmount)
        {
            var remainingRefundableAmount =
                RoundMoney(
                    payment.Amount
                    -
                    payment.RefundedAmount);

            if (requestedRefundAmount >
                remainingRefundableAmount)
            {
                throw new InvalidOperationException(
                    "The requested refund amount exceeds the remaining refundable payment amount.");
            }
        }

        private static void ValidateRefundablePayment(
            BookingPayment payment)
        {
            if (!string.Equals(
                    payment.Provider,
                    StripeProvider,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only Stripe payments can be refunded by this service.");
            }

            if (string.IsNullOrWhiteSpace(
                    payment.ProviderPaymentId))
            {
                throw new InvalidOperationException(
                    "The payment is missing the Stripe PaymentIntent identifier.");
            }

            if (payment.Status is not
                PaymentStatus.Succeeded
                and not
                PaymentStatus.PartiallyRefunded)
            {
                throw new InvalidOperationException(
                    "The payment is not in a refundable status.");
            }

            if (payment.Amount <= 0)
            {
                throw new InvalidOperationException(
                    "The payment amount is invalid.");
            }

            if (payment.RefundedAmount < 0
                ||
                payment.RefundedAmount >
                    payment.Amount)
            {
                throw new InvalidOperationException(
                    "The payment refunded amount is invalid.");
            }

            if (payment.RefundedAmount ==
                payment.Amount)
            {
                throw new InvalidOperationException(
                    "The payment has already been fully refunded.");
            }

            var normalizedCurrency =
                StripeAmountConverter.NormalizeCurrency(
                    payment.Currency);

            _ =
                StripeAmountConverter.ToMinorUnit(
                    payment.Amount,
                    normalizedCurrency);
        }

        private static void EnsureExistingRefundMatches(
            LocalRefundData existingRefund,
            Guid? expectedPaymentId,
            Guid expectedBookingId,
            decimal expectedAmount,
            string operationName)
        {
            if (expectedPaymentId.HasValue
                &&
                existingRefund.PaymentId !=
                    expectedPaymentId.Value)
            {
                throw new InvalidOperationException(
                    $"The existing {operationName} refund belongs to a different payment.");
            }

            if (existingRefund.BookingId !=
                expectedBookingId)
            {
                throw new InvalidOperationException(
                    $"The existing {operationName} refund belongs to a different booking.");
            }

            if (existingRefund.Amount !=
                expectedAmount)
            {
                throw new InvalidOperationException(
                    $"The existing {operationName} refund uses a different amount.");
            }
        }

        private static void ValidateLocalRefundForProviderCall(
            LocalRefundData refund)
        {
            if (!string.Equals(
                    refund.Provider,
                    StripeProvider,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only Stripe refunds are supported.");
            }

            if (string.IsNullOrWhiteSpace(
                    refund.ProviderPaymentId))
            {
                throw new InvalidOperationException(
                    "The local refund is missing the Stripe PaymentIntent identifier.");
            }

            if (refund.Amount <= 0)
            {
                throw new InvalidOperationException(
                    "The local refund amount is invalid.");
            }

            var normalizedCurrency =
                StripeAmountConverter.NormalizeCurrency(
                    refund.Currency);

            _ =
                StripeAmountConverter.ToMinorUnit(
                    refund.Amount,
                    normalizedCurrency);

            if (string.IsNullOrWhiteSpace(
                    refund.ProviderIdempotencyKey))
            {
                throw new InvalidOperationException(
                    "The refund idempotency key is missing.");
            }
        }

        private static void ValidateStripeRefundResult(
            BookingPaymentRefund localRefund,
            StripeRefundResult stripeRefund)
        {
            if (string.IsNullOrWhiteSpace(
                    stripeRefund.RefundId))
            {
                throw new PaymentProviderException(
                    "Stripe returned an invalid refund identifier.",
                    StripeProvider);
            }

            if (!string.IsNullOrWhiteSpace(
                    localRefund.ProviderRefundId)
                &&
                !string.Equals(
                    localRefund.ProviderRefundId,
                    stripeRefund.RefundId,
                    StringComparison.Ordinal))
            {
                throw new PaymentProviderException(
                    "Stripe returned a refund identifier that does not match the local refund record.",
                    StripeProvider);
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
                    "Stripe returned an unexpected refund currency.",
                    StripeProvider);
            }

            var expectedAmountInMinorUnit =
                StripeAmountConverter.ToMinorUnit(
                    localRefund.Amount,
                    expectedCurrency);

            if (stripeRefund.AmountInMinorUnit !=
                expectedAmountInMinorUnit)
            {
                throw new PaymentProviderException(
                    "Stripe returned an unexpected refund amount.",
                    StripeProvider);
            }
        }

        // =====================================================
        // General helpers
        // =====================================================

        private static decimal NormalizeRefundAmount(
            decimal refundAmount)
        {
            var normalizedAmount =
                RoundMoney(refundAmount);

            if (normalizedAmount <= 0)
            {
                throw new ArgumentException(
                    "The refund amount must be greater than zero.");
            }

            return normalizedAmount;
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

        private static string NormalizeFailureReason(
            string? failureReason)
        {
            if (string.IsNullOrWhiteSpace(
                    failureReason))
            {
                return "unknown";
            }

            var normalizedReason =
                failureReason.Trim();

            return normalizedReason.Length <=
                MaximumFailureReasonLength
                    ? normalizedReason
                    : normalizedReason[
                        ..MaximumFailureReasonLength];
        }

        private static string
            BuildSupportTicketRefundIdempotencyKey(
                Guid supportTicketId)
        {
            return
                $"support-ticket-refund:{supportTicketId:N}";
        }

        private static string
            BuildBookingCancellationRefundIdempotencyKey(
                Guid bookingId)
        {
            return
                $"booking-cancellation-refund:{bookingId:N}";
        }

        private static DateTimeOffset ResolveTerminalTimestamp(
            DateTimeOffset localCreatedAt,
            DateTimeOffset providerCreatedAt,
            DateTimeOffset currentTime)
        {
            return providerCreatedAt >= localCreatedAt
                ? providerCreatedAt
                : EnsureNotBeforeCreatedAt(
                    localCreatedAt,
                    currentTime);
        }

        private static DateTimeOffset EnsureNotBeforeCreatedAt(
            DateTimeOffset createdAt,
            DateTimeOffset timestamp)
        {
            return timestamp >= createdAt
                ? timestamp
                : createdAt;
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static void ValidateAdminUserIdentifier(
            Guid adminUserId)
        {
            if (adminUserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain a valid admin identifier.");
            }
        }

        private static void ValidateSupportTicketIdentifier(
            Guid supportTicketId)
        {
            if (supportTicketId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The support ticket identifier is invalid.");
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

        private static void ValidateBookingIdentifier(
            Guid bookingId)
        {
            if (bookingId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The booking identifier is invalid.");
            }
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

        // =====================================================
        // Internal models
        // =====================================================

        private sealed class SupportTicketRefundContext
        {
            public Guid? BookingId { get; init; }

            public SupportTicketStatus Status { get; init; }

            public SupportTicketDecisionStatus DecisionStatus
            { get; init; }

            public SupportTicketDecisionAction DecisionAction
            { get; init; }

            public DateTimeOffset? DecidedAt { get; init; }

            public Guid? DecidedByAdminId { get; init; }
        }

        private sealed class LocalRefundData
        {
            public Guid RefundId { get; init; }

            public Guid PaymentId { get; init; }

            public Guid BookingId { get; init; }

            public Guid GuestUserId { get; init; }

            public decimal Amount { get; init; }

            public string Currency { get; init; } =
                string.Empty;

            public string Provider { get; init; } =
                string.Empty;

            public string ProviderPaymentId { get; init; } =
                string.Empty;

            public string? ProviderRefundId { get; init; }

            public PaymentRefundStatus Status { get; init; }

            public string ProviderIdempotencyKey { get; init; } =
                string.Empty;

            public bool WasAlreadyProcessed { get; set; }
        }
    }
}