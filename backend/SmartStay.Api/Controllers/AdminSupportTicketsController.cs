using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/support/tickets")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminSupportTicketsController : ControllerBase
    {
        private const int MaximumRefundNoteLength = 1000;

        private readonly ISupportTicketService _supportTicketService;
        private readonly IBookingPayoutService _bookingPayoutService;
        private readonly IPaymentRefundService _paymentRefundService;
        private readonly IAdminActionLogService _adminActionLogService;
        private readonly SmartStayDbContext _dbContext;
        private readonly ILogger<AdminSupportTicketsController> _logger;

        public AdminSupportTicketsController(
            ISupportTicketService supportTicketService,
            IBookingPayoutService bookingPayoutService,
            IPaymentRefundService paymentRefundService,
            IAdminActionLogService adminActionLogService,
            SmartStayDbContext dbContext,
            ILogger<AdminSupportTicketsController> logger)
        {
            ArgumentNullException.ThrowIfNull(supportTicketService);
            ArgumentNullException.ThrowIfNull(bookingPayoutService);
            ArgumentNullException.ThrowIfNull(paymentRefundService);
            ArgumentNullException.ThrowIfNull(adminActionLogService);
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(logger);

            _supportTicketService = supportTicketService;
            _bookingPayoutService = bookingPayoutService;
            _paymentRefundService = paymentRefundService;
            _adminActionLogService = adminActionLogService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(SupportTicketsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SupportTicketsResponse>>
            GetTicketsAsync(
                [FromQuery] SupportTicketSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var response =
                await _supportTicketService.GetAdminTicketsAsync(
                    request,
                    cancellationToken);

            return Ok(response);
        }

        [HttpGet("{ticketId:guid}")]
        [ProducesResponseType(
            typeof(SupportTicketResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupportTicketResponse>>
            GetTicketByIdAsync(
                Guid ticketId,
                CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(ticketId);

            var response =
                await _supportTicketService.GetAdminTicketByIdAsync(
                    ticketId,
                    cancellationToken);

            return Ok(response);
        }

        [HttpPost("{ticketId:guid}/reply")]
        [ProducesResponseType(
            typeof(SupportTicketResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupportTicketResponse>>
            ReplyAsync(
                Guid ticketId,
                CreateSupportTicketMessageRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(request);

            var adminUserId = GetCurrentUserId();

            var response =
                await _supportTicketService.AddAdminReplyAsync(
                    adminUserId,
                    ticketId,
                    request,
                    cancellationToken);

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.Replied.ToString(),

                    TargetType =
                        AdminActionTargetType.SupportTicket.ToString(),

                    TargetId =
                        response.TicketId,

                    TargetReference =
                        response.ReferenceCode,

                    Summary =
                        $"Admin replied to support ticket {response.ReferenceCode}.",

                    Details =
                        request.Message,

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            return Ok(response);
        }

        [HttpPatch("{ticketId:guid}/decision")]
        [ProducesResponseType(
            typeof(SupportTicketResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupportTicketResponse>>
            ApplyDecisionAsync(
                Guid ticketId,
                ApplySupportTicketDecisionRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(request);

            var adminUserId = GetCurrentUserId();

            var decisionContext =
                await NormalizeAndValidateDecisionRequestAsync(
                    ticketId,
                    request,
                    cancellationToken);

            var response =
                await _supportTicketService.ApplyAdminDecisionAsync(
                    adminUserId,
                    ticketId,
                    request,
                    cancellationToken);

            var payoutOutcome =
                await FinalizeDecisionPayoutPolicyAsync(
                    decisionContext,
                    response,
                    cancellationToken);

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.DecisionApplied.ToString(),

                    TargetType =
                        AdminActionTargetType.SupportTicket.ToString(),

                    TargetId =
                        response.TicketId,

                    TargetReference =
                        response.ReferenceCode,

                    Summary =
                        $"Admin applied decision '{response.DecisionStatus}' " +
                        $"to support ticket {response.ReferenceCode}.",

                    Details =
                        $"DecisionStatus: {response.DecisionStatus}; " +
                        $"DecisionAction: {response.DecisionAction}; " +
                        $"ResolveTicket: {request.ResolveTicket}; " +
                        $"DecisionNote: {request.DecisionNote ?? "N/A"}",

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            await TryCreatePayoutActionLogAsync(
                adminUserId,
                response,
                payoutOutcome,
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("{ticketId:guid}/refund")]
        [ProducesResponseType(
            typeof(PaymentRefundResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<PaymentRefundResponse>>
            ExecuteRefundAsync(
                Guid ticketId,
                CreateSupportTicketRefundRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(request);

            var adminUserId = GetCurrentUserId();

            var refundContext =
                await BuildRefundExecutionContextAsync(
                    ticketId,
                    request,
                    cancellationToken);

            var refundResponse =
                await _paymentRefundService.CreateSupportTicketRefundAsync(
                    adminUserId,
                    ticketId,
                    refundContext.RefundAmount,
                    cancellationToken);

            PayoutRefundReconciliationOutcome? reconciliationOutcome = null;

            if (HasRefundStatus(
                    refundResponse,
                    PaymentRefundStatus.Succeeded))
            {
                reconciliationOutcome =
                    await TryReconcilePayoutAfterSuccessfulRefundAsync(
                        refundContext,
                        refundResponse,
                        cancellationToken);
            }

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.Other.ToString(),

                    TargetType =
                        AdminActionTargetType.Refund.ToString(),

                    TargetId =
                        refundResponse.RefundId,

                    TargetReference =
                        refundContext.Ticket.ReferenceCode,

                    Summary =
                        $"Admin processed a {refundContext.DecisionAction} " +
                        $"for support ticket {refundContext.Ticket.ReferenceCode}.",

                    Details =
                        BuildRefundActionLogDetails(
                            refundContext,
                            refundResponse),

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            if (reconciliationOutcome is not null)
            {
                await TryCreateAdminActionLogAsync(
                    adminUserId,
                    BuildPayoutActionLogRequest(
                        reconciliationOutcome.ActionType,
                        reconciliationOutcome.Payout,
                        refundContext.Ticket,
                        reconciliationOutcome.Details),
                    cancellationToken);

                refundResponse.Message =
                    $"{refundResponse.Message} " +
                    reconciliationOutcome.ResponseMessage;
            }

            return Ok(refundResponse);
        }

        [HttpPatch("{ticketId:guid}/resolve")]
        [ProducesResponseType(
            typeof(SupportTicketResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupportTicketResponse>>
            ResolveAsync(
                Guid ticketId,
                ResolveSupportTicketRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(request);

            var adminUserId = GetCurrentUserId();

            var resolutionContext =
                await ValidateStandaloneResolutionAsync(
                    ticketId,
                    cancellationToken);

            var response =
                await _supportTicketService.ResolveTicketAsync(
                    adminUserId,
                    ticketId,
                    request,
                    cancellationToken);

            BookingPayoutResponse? releasedPayout = null;

            if (resolutionContext.ShouldReleaseHeldPayout
                &&
                response.BookingId.HasValue)
            {
                releasedPayout =
                    await TryReleaseHeldPayoutAsync(
                        response.BookingId.Value,
                        request.ResolutionNote
                        ??
                        $"Support ticket {response.ReferenceCode} " +
                        "was resolved without a pending financial remedy.",
                        cancellationToken);
            }

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.Resolved.ToString(),

                    TargetType =
                        AdminActionTargetType.SupportTicket.ToString(),

                    TargetId =
                        response.TicketId,

                    TargetReference =
                        response.ReferenceCode,

                    Summary =
                        $"Admin resolved support ticket " +
                        $"{response.ReferenceCode}.",

                    Details =
                        request.ResolutionNote,

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            if (releasedPayout is not null)
            {
                await TryCreateAdminActionLogAsync(
                    adminUserId,
                    BuildPayoutActionLogRequest(
                        AdminActionType.PayoutReleased,
                        releasedPayout,
                        response,
                        "The held payout was released because the " +
                        "support ticket was resolved without a " +
                        "pending financial remedy."),
                    cancellationToken);
            }

            return Ok(response);
        }

        private async Task<DecisionContext>
            NormalizeAndValidateDecisionRequestAsync(
                Guid ticketId,
                ApplySupportTicketDecisionRequest request,
                CancellationToken cancellationToken)
        {
            var ticket =
                await _supportTicketService.GetAdminTicketByIdAsync(
                    ticketId,
                    cancellationToken);

            var decisionStatus =
                ParseCanonicalDecisionStatus(
                    request.DecisionStatus);

            var requestedAction =
                ParseCanonicalDecisionAction(
                    request.DecisionAction);

            var decisionNote =
                request.DecisionNote?.Trim();

            var adminMessage =
                request.AdminMessage?.Trim();

            if (string.IsNullOrWhiteSpace(decisionNote)
                ||
                decisionNote.Length < 5)
            {
                throw new ArgumentException(
                    "The decision note must contain at least 5 characters.");
            }

            if (decisionNote.Length > 1000)
            {
                throw new ArgumentException(
                    "The decision note cannot exceed 1000 characters.");
            }

            if (adminMessage?.Length > 4000)
            {
                throw new ArgumentException(
                    "The user message cannot exceed 4000 characters.");
            }

            BookingPayoutResponse? payout = null;

            if (ticket.BookingId.HasValue)
            {
                payout =
                    await _bookingPayoutService.GetByBookingIdAsync(
                        ticket.BookingId.Value,
                        cancellationToken);
            }

            var effectiveAction = requestedAction;

            switch (decisionStatus)
            {
                case SupportTicketDecisionStatus.NeedsMoreEvidence:
                    if (request.ResolveTicket)
                    {
                        throw new ArgumentException(
                            "A ticket that needs more evidence " +
                            "cannot be resolved.");
                    }

                    if (string.IsNullOrWhiteSpace(adminMessage)
                        ||
                        adminMessage.Length < 5)
                    {
                        throw new ArgumentException(
                            "Explain which evidence is required " +
                            "in the message to the user.");
                    }

                    effectiveAction =
                        IsPayoutHoldable(payout)
                            ? SupportTicketDecisionAction
                                .HoldPayoutRecommended
                            : SupportTicketDecisionAction
                                .NoAction;
                    break;

                case SupportTicketDecisionStatus.InvalidComplaint:
                    if (IsPayoutBlocked(payout))
                    {
                        throw new InvalidOperationException(
                            "The complaint cannot be rejected while " +
                            "its payout is blocked for a pending " +
                            "financial remedy. Complete or reverse " +
                            "the financial remedy first.");
                    }

                    effectiveAction =
                        IsPayoutHeld(payout)
                            ? SupportTicketDecisionAction
                                .ReleasePayoutRecommended
                            : SupportTicketDecisionAction
                                .NoAction;
                    break;

                case SupportTicketDecisionStatus.ValidComplaint:
                    ValidateValidComplaintAction(
                        requestedAction,
                        ticket,
                        payout,
                        request.ResolveTicket);
                    break;

                case SupportTicketDecisionStatus.NoDecision:
                default:
                    throw new ArgumentException(
                        "A real support ticket decision must be selected.");
            }

            request.DecisionStatus =
                decisionStatus.ToString();

            request.DecisionAction =
                effectiveAction.ToString();

            request.DecisionNote =
                decisionNote;

            request.AdminMessage =
                string.IsNullOrWhiteSpace(adminMessage)
                    ? null
                    : adminMessage;

            return new DecisionContext
            {
                EffectiveAction =
                    effectiveAction,

                ShouldReleaseHeldPayoutAfterResolution =
                    decisionStatus ==
                        SupportTicketDecisionStatus.ValidComplaint
                    &&
                    request.ResolveTicket
                    &&
                    effectiveAction is
                        SupportTicketDecisionAction.NoAction
                        or SupportTicketDecisionAction
                            .HostWarningRecommended
                        or SupportTicketDecisionAction
                            .HidePropertyRecommended
            };
        }

        private static void ValidateValidComplaintAction(
            SupportTicketDecisionAction action,
            SupportTicketResponse ticket,
            BookingPayoutResponse? payout,
            bool resolveTicket)
        {
            if (action ==
                SupportTicketDecisionAction.ReleasePayoutRecommended)
            {
                throw new ArgumentException(
                    "Release payout is not a valid action for an " +
                    "accepted complaint. Use it when rejecting a complaint.");
            }

            var requiresBooking =
                action is
                    SupportTicketDecisionAction.PartialRefundRecommended
                    or SupportTicketDecisionAction.FullRefundRecommended
                    or SupportTicketDecisionAction.HoldPayoutRecommended;

            if (requiresBooking
                &&
                !ticket.BookingId.HasValue)
            {
                throw new ArgumentException(
                    "The selected financial decision requires " +
                    "a ticket linked to a booking.");
            }

            if (requiresBooking
                &&
                payout is null)
            {
                throw new InvalidOperationException(
                    "The selected financial decision cannot be applied " +
                    "because this booking does not have a payout record.");
            }

            if (action ==
                    SupportTicketDecisionAction.HoldPayoutRecommended
                &&
                !IsPayoutHoldable(payout))
            {
                throw new InvalidOperationException(
                    "The payout cannot be held in its current status.");
            }

            if (action is
                    SupportTicketDecisionAction.PartialRefundRecommended
                    or SupportTicketDecisionAction.FullRefundRecommended
                &&
                !IsPayoutBlockable(payout))
            {
                throw new InvalidOperationException(
                    "A refund recommendation cannot protect this payout " +
                    "because it is already paid or refunded.");
            }

            var isNonFinancialFinalAction =
                action is
                    SupportTicketDecisionAction.NoAction
                    or SupportTicketDecisionAction
                        .HostWarningRecommended
                    or SupportTicketDecisionAction
                        .HidePropertyRecommended;

            if (isNonFinancialFinalAction
                &&
                IsPayoutBlocked(payout))
            {
                throw new InvalidOperationException(
                    "A non-financial final decision cannot be applied " +
                    "while the related payout is blocked for a pending " +
                    "financial remedy.");
            }

            if (resolveTicket
                &&
                action is
                    SupportTicketDecisionAction.PartialRefundRecommended
                    or SupportTicketDecisionAction.FullRefundRecommended
                    or SupportTicketDecisionAction.HoldPayoutRecommended)
            {
                throw new ArgumentException(
                    "The ticket must remain in progress while a payout " +
                    "hold or refund recommendation is pending.");
            }
        }

        private async Task<RefundExecutionContext>
            BuildRefundExecutionContextAsync(
                Guid ticketId,
                CreateSupportTicketRefundRequest request,
                CancellationToken cancellationToken)
        {
            var ticket =
                await _supportTicketService.GetAdminTicketByIdAsync(
                    ticketId,
                    cancellationToken);

            if (!string.Equals(
                    ticket.Status,
                    SupportTicketStatus.InProgress.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                &&
                !string.Equals(
                    ticket.Status,
                    SupportTicketStatus.Open.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "A refund cannot be executed for a resolved or closed " +
                    "support ticket.");
            }

            var decisionStatus =
                ParseCanonicalDecisionStatus(
                    ticket.DecisionStatus);

            if (decisionStatus !=
                SupportTicketDecisionStatus.ValidComplaint)
            {
                throw new InvalidOperationException(
                    "Only a valid complaint can execute a refund.");
            }

            var decisionAction =
                ParseCanonicalDecisionAction(
                    ticket.DecisionAction);

            if (decisionAction is not
                SupportTicketDecisionAction.PartialRefundRecommended
                and not SupportTicketDecisionAction.FullRefundRecommended)
            {
                throw new InvalidOperationException(
                    "The support ticket decision does not recommend a refund.");
            }

            if (!ticket.BookingId.HasValue
                ||
                ticket.BookingId.Value == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The support ticket is not linked to a valid booking.");
            }

            var payout =
                await _bookingPayoutService.GetByBookingIdAsync(
                    ticket.BookingId.Value,
                    cancellationToken);

            if (payout is null)
            {
                throw new InvalidOperationException(
                    "The booking does not have a payout record.");
            }

            if (!HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Blocked))
            {
                throw new InvalidOperationException(
                    "The booking payout must be blocked before the refund " +
                    "is executed.");
            }

            var payment =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == payout.BookingPaymentId
                        &&
                        item.BookingId == ticket.BookingId.Value)
                    .Select(item =>
                        new RefundablePaymentSnapshot
                        {
                            PaymentId =
                                item.Id,

                            Amount =
                                item.Amount,

                            RefundedAmount =
                                item.RefundedAmount,

                            Currency =
                                item.Currency,

                            Status =
                                item.Status
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (payment is null)
            {
                throw new KeyNotFoundException(
                    "The booking payment linked to this payout was not found.");
            }

            if (payment.Status is not
                PaymentStatus.Succeeded
                and not PaymentStatus.PartiallyRefunded)
            {
                throw new InvalidOperationException(
                    "The booking payment is not in a refundable status.");
            }

            var remainingRefundableAmount =
                RoundMoney(
                    payment.Amount
                    -
                    payment.RefundedAmount);

            if (remainingRefundableAmount <= 0)
            {
                throw new InvalidOperationException(
                    "The booking payment has no remaining refundable amount.");
            }

            var refundAmount =
                ResolveRequestedRefundAmount(
                    decisionAction,
                    request.RefundAmount,
                    remainingRefundableAmount);

            request.RefundAmount =
                refundAmount;

            request.RefundNote =
                NormalizeOptionalText(
                    request.RefundNote,
                    MaximumRefundNoteLength);

            return new RefundExecutionContext
            {
                Ticket =
                    ticket,

                DecisionAction =
                    decisionAction,

                BookingId =
                    ticket.BookingId.Value,

                PaymentId =
                    payment.PaymentId,

                PayoutId =
                    payout.PayoutId,

                PayoutAmountBeforeRefund =
                    payout.Amount,

                RefundAmount =
                    refundAmount,

                RemainingRefundableAmountBeforeRefund =
                    remainingRefundableAmount,

                Currency =
                    payment.Currency,

                RefundNote =
                    request.RefundNote
            };
        }

        private async Task<PayoutRefundReconciliationOutcome?>
            TryReconcilePayoutAfterSuccessfulRefundAsync(
                RefundExecutionContext context,
                PaymentRefundResponse refundResponse,
                CancellationToken cancellationToken)
        {
            var paymentStatus =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == context.PaymentId
                        &&
                        item.BookingId == context.BookingId)
                    .Select(item =>
                        item.Status)
                    .SingleOrDefaultAsync(
                        cancellationToken);

            try
            {
                if (context.DecisionAction ==
                    SupportTicketDecisionAction.FullRefundRecommended)
                {
                    if (paymentStatus != PaymentStatus.Refunded)
                    {
                        return null;
                    }

                    var payout =
                        await _bookingPayoutService
                            .MarkPayoutRefundedForBookingAsync(
                                context.BookingId,
                                context.RefundNote
                                ??
                                $"Full refund {refundResponse.RefundId} " +
                                $"was completed for support ticket " +
                                $"{context.Ticket.ReferenceCode}.",
                                cancellationToken);

                    return new PayoutRefundReconciliationOutcome
                    {
                        ActionType =
                            AdminActionType.PayoutMarkedRefunded,

                        Payout =
                            payout,

                        Details =
                            "The full support-ticket refund succeeded and " +
                            "the host payout was reconciled as refunded.",

                        ResponseMessage =
                            "The related host payout was marked as refunded."
                    };
                }

                if (context.DecisionAction ==
                    SupportTicketDecisionAction.PartialRefundRecommended)
                {
                    if (paymentStatus != PaymentStatus.PartiallyRefunded)
                    {
                        return null;
                    }

                    var payout =
                        await _bookingPayoutService
                            .ReconcilePartialRefundForBookingAsync(
                                context.BookingId,
                                refundResponse.RefundId,
                                context.RefundNote
                                ??
                                $"Partial refund {refundResponse.RefundId} " +
                                $"was reconciled for support ticket " +
                                $"{context.Ticket.ReferenceCode}.",
                                cancellationToken);

                    return new PayoutRefundReconciliationOutcome
                    {
                        ActionType =
                            AdminActionType.Updated,

                        Payout =
                            payout,

                        Details =
                            $"The successful partial refund adjusted the " +
                            $"host payout from " +
                            $"{context.PayoutAmountBeforeRefund:0.00} " +
                            $"{context.Currency} to " +
                            $"{payout.Amount:0.00} {payout.Currency}, then " +
                            $"returned it to {payout.Status} according to " +
                            $"its original availability schedule.",

                        ResponseMessage =
                            $"The host payout was adjusted to " +
                            $"{payout.Amount:0.00} {payout.Currency} and " +
                            $"returned to {payout.Status}."
                    };
                }

                return null;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Refund {RefundId} succeeded for support ticket " +
                    "{TicketId}, but payout reconciliation failed for " +
                    "booking {BookingId}. The operation can be retried " +
                    "safely because the refund is idempotent.",
                    refundResponse.RefundId,
                    context.Ticket.TicketId,
                    context.BookingId);

                refundResponse.Message =
                    $"{refundResponse.Message} " +
                    "The refund succeeded, but payout reconciliation is " +
                    "still pending. Retry this operation to reconcile it.";

                return null;
            }
        }

        private async Task<ResolutionContext>
            ValidateStandaloneResolutionAsync(
                Guid ticketId,
                CancellationToken cancellationToken)
        {
            var ticket =
                await _supportTicketService.GetAdminTicketByIdAsync(
                    ticketId,
                    cancellationToken);

            var decisionStatus =
                ParseCanonicalDecisionStatus(
                    ticket.DecisionStatus);

            var decisionAction =
                ParseCanonicalDecisionAction(
                    ticket.DecisionAction);

            if (decisionStatus is
                SupportTicketDecisionStatus.NoDecision
                or SupportTicketDecisionStatus.NeedsMoreEvidence)
            {
                throw new InvalidOperationException(
                    "The ticket must receive a final complaint decision " +
                    "before it can be resolved.");
            }

            BookingPayoutResponse? payout = null;

            if (ticket.BookingId.HasValue)
            {
                payout =
                    await _bookingPayoutService.GetByBookingIdAsync(
                        ticket.BookingId.Value,
                        cancellationToken);
            }

            if (decisionAction ==
                SupportTicketDecisionAction.HoldPayoutRecommended)
            {
                throw new InvalidOperationException(
                    "Apply a final non-hold decision before " +
                    "resolving this ticket.");
            }

            if (decisionAction ==
                SupportTicketDecisionAction.FullRefundRecommended)
            {
                if (!IsPayoutRefunded(payout))
                {
                    throw new InvalidOperationException(
                        "The ticket cannot be resolved until the full " +
                        "refund and payout reconciliation are completed.");
                }

                return new ResolutionContext
                {
                    ShouldReleaseHeldPayout =
                        false
                };
            }

            if (decisionAction ==
                SupportTicketDecisionAction.PartialRefundRecommended)
            {
                await ValidatePartialRefundResolutionAsync(
                    ticket,
                    payout,
                    cancellationToken);

                return new ResolutionContext
                {
                    ShouldReleaseHeldPayout =
                        false
                };
            }

            if (IsPayoutBlocked(payout))
            {
                throw new InvalidOperationException(
                    "The ticket cannot be resolved while " +
                    "its payout is blocked.");
            }

            return new ResolutionContext
            {
                ShouldReleaseHeldPayout =
                    IsPayoutHeld(payout)
            };
        }

        private async Task ValidatePartialRefundResolutionAsync(
            SupportTicketResponse ticket,
            BookingPayoutResponse? payout,
            CancellationToken cancellationToken)
        {
            if (!ticket.BookingId.HasValue
                ||
                ticket.BookingId.Value == Guid.Empty
                ||
                payout is null)
            {
                throw new InvalidOperationException(
                    "The partial refund ticket is not linked to a valid " +
                    "booking payout.");
            }

            if (!IsOpenPayoutStatus(payout))
            {
                throw new InvalidOperationException(
                    "The ticket cannot be resolved until the partially " +
                    "refunded payout is reconciled and returned to " +
                    "Pending or Available.");
            }

            if (!payout.BlockedAt.HasValue
                ||
                !payout.ReleasedAt.HasValue)
            {
                throw new InvalidOperationException(
                    "The payout does not contain a completed partial-refund " +
                    "reconciliation history.");
            }

            var snapshot =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == payout.BookingPaymentId
                        &&
                        item.BookingId == ticket.BookingId.Value)
                    .Select(item =>
                        new PartialRefundResolutionSnapshot
                        {
                            PaymentAmount =
                                item.Amount,

                            RefundedAmount =
                                item.RefundedAmount,

                            PaymentStatus =
                                item.Status,

                            BookingSubtotal =
                                item.Booking.Subtotal,

                            SuccessfulRefundAmount =
                                item.Refunds
                                    .Where(refund =>
                                        refund.Status ==
                                            PaymentRefundStatus.Succeeded)
                                    .Select(refund =>
                                        (decimal?)refund.Amount)
                                    .Sum()
                                ??
                                0m
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (snapshot is null)
            {
                throw new KeyNotFoundException(
                    "The payment linked to the partially refunded payout " +
                    "was not found.");
            }

            var paymentAmount =
                RoundMoney(snapshot.PaymentAmount);

            var refundedAmount =
                RoundMoney(snapshot.RefundedAmount);

            var successfulRefundAmount =
                RoundMoney(snapshot.SuccessfulRefundAmount);

            if (snapshot.PaymentStatus !=
                    PaymentStatus.PartiallyRefunded
                ||
                paymentAmount <= 0
                ||
                refundedAmount <= 0
                ||
                refundedAmount >= paymentAmount
                ||
                successfulRefundAmount != refundedAmount)
            {
                throw new InvalidOperationException(
                    "The payment does not contain a complete and verified " +
                    "partial refund.");
            }

            var originalHostPayoutAmount =
                snapshot.BookingSubtotal > 0
                    ? RoundMoney(snapshot.BookingSubtotal)
                    : paymentAmount;

            var remainingPaymentAmount =
                RoundMoney(
                    paymentAmount
                    -
                    refundedAmount);

            var expectedHostPayoutAmount =
                RoundMoney(
                    originalHostPayoutAmount
                    *
                    remainingPaymentAmount
                    /
                    paymentAmount);

            if (expectedHostPayoutAmount <= 0
                ||
                RoundMoney(payout.Amount) !=
                    expectedHostPayoutAmount)
            {
                throw new InvalidOperationException(
                    "The host payout amount has not been reconciled with " +
                    "the verified partial refund.");
            }
        }

        private async Task<PayoutDecisionOutcome?>
            FinalizeDecisionPayoutPolicyAsync(
                DecisionContext context,
                SupportTicketResponse ticket,
                CancellationToken cancellationToken)
        {
            if (!ticket.BookingId.HasValue)
            {
                return null;
            }

            if (context.ShouldReleaseHeldPayoutAfterResolution)
            {
                var releasedPayout =
                    await TryReleaseHeldPayoutAsync(
                        ticket.BookingId.Value,
                        ticket.DecisionNote
                        ??
                        $"Support ticket {ticket.ReferenceCode} " +
                        "was resolved without a pending financial remedy.",
                        cancellationToken);

                if (releasedPayout is not null)
                {
                    return new PayoutDecisionOutcome
                    {
                        ActionType =
                            AdminActionType.PayoutReleased,

                        Payout =
                            releasedPayout,

                        Details =
                            "The held payout was released because " +
                            "the accepted complaint was resolved " +
                            "without a refund or payout-hold remedy."
                    };
                }
            }

            var currentPayout =
                await _bookingPayoutService.GetByBookingIdAsync(
                    ticket.BookingId.Value,
                    cancellationToken);

            if (currentPayout is null)
            {
                return null;
            }

            return context.EffectiveAction switch
            {
                SupportTicketDecisionAction.HoldPayoutRecommended
                    when HasPayoutStatus(
                        currentPayout,
                        BookingPayoutStatus.Held) =>
                    new PayoutDecisionOutcome
                    {
                        ActionType =
                            AdminActionType.PayoutHeld,

                        Payout =
                            currentPayout,

                        Details =
                            "The payout remains held while the " +
                            "support ticket requires more evidence " +
                            "or investigation."
                    },

                SupportTicketDecisionAction.ReleasePayoutRecommended
                    when IsPayoutReleasedFromHold(currentPayout) =>
                    new PayoutDecisionOutcome
                    {
                        ActionType =
                            AdminActionType.PayoutReleased,

                        Payout =
                            currentPayout,

                        Details =
                            "The complaint hold was released. " +
                            "The payout returned to Pending when its " +
                            "original availability date is still in " +
                            "the future, otherwise it became Available."
                    },

                SupportTicketDecisionAction.PartialRefundRecommended
                    or SupportTicketDecisionAction.FullRefundRecommended
                    when HasPayoutStatus(
                        currentPayout,
                        BookingPayoutStatus.Blocked) =>
                    new PayoutDecisionOutcome
                    {
                        ActionType =
                            AdminActionType.PayoutBlocked,

                        Payout =
                            currentPayout,

                        Details =
                            "The payout was blocked while the recommended " +
                            "refund is awaiting processing."
                    },

                _ =>
                    null
            };
        }

        private async Task<BookingPayoutResponse?>
            TryReleaseHeldPayoutAsync(
                Guid bookingId,
                string releaseNote,
                CancellationToken cancellationToken)
        {
            try
            {
                var payout =
                    await _bookingPayoutService.GetByBookingIdAsync(
                        bookingId,
                        cancellationToken);

                if (!IsPayoutHeld(payout))
                {
                    return null;
                }

                return await _bookingPayoutService
                    .ReleasePayoutForBookingAsync(
                        bookingId,
                        releaseNote,
                        cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Could not release held payout for booking " +
                    "{BookingId} while finalizing a support ticket.",
                    bookingId);

                return null;
            }
        }

        private async Task TryCreatePayoutActionLogAsync(
            Guid adminUserId,
            SupportTicketResponse ticket,
            PayoutDecisionOutcome? outcome,
            CancellationToken cancellationToken)
        {
            if (outcome is null)
            {
                return;
            }

            await TryCreateAdminActionLogAsync(
                adminUserId,
                BuildPayoutActionLogRequest(
                    outcome.ActionType,
                    outcome.Payout,
                    ticket,
                    outcome.Details),
                cancellationToken);
        }

        private CreateAdminActionLogRequest
            BuildPayoutActionLogRequest(
                AdminActionType actionType,
                BookingPayoutResponse payout,
                SupportTicketResponse ticket,
                string details)
        {
            return new CreateAdminActionLogRequest
            {
                ActionType =
                    actionType.ToString(),

                TargetType =
                    AdminActionTargetType.Payout.ToString(),

                TargetId =
                    payout.PayoutId,

                TargetReference =
                    payout.BookingId.ToString(),

                Summary =
                    $"Support ticket {ticket.ReferenceCode} " +
                    $"changed payout {payout.PayoutId} " +
                    $"to {payout.Status}.",

                Details =
                    details,

                IpAddress =
                    GetClientIpAddress(),

                UserAgent =
                    GetUserAgent()
            };
        }

        private static string BuildRefundActionLogDetails(
            RefundExecutionContext context,
            PaymentRefundResponse refund)
        {
            var details =
                $"SupportTicketId: {context.Ticket.TicketId}; " +
                $"BookingId: {context.BookingId}; " +
                $"PaymentId: {context.PaymentId}; " +
                $"PayoutId: {context.PayoutId}; " +
                $"DecisionAction: {context.DecisionAction}; " +
                $"RefundAmount: {context.RefundAmount:0.00} " +
                $"{context.Currency}; " +
                $"RemainingBeforeRefund: " +
                $"{context.RemainingRefundableAmountBeforeRefund:0.00} " +
                $"{context.Currency}; " +
                $"RefundStatus: {refund.Status}; " +
                $"ProviderRefundId: {refund.ProviderRefundId ?? "N/A"}; " +
                $"WasAlreadyProcessed: {refund.WasAlreadyProcessed}.";

            if (!string.IsNullOrWhiteSpace(context.RefundNote))
            {
                details +=
                    $" AdminNote: {context.RefundNote}";
            }

            return details;
        }

        private static decimal ResolveRequestedRefundAmount(
            SupportTicketDecisionAction decisionAction,
            decimal? requestedAmount,
            decimal remainingRefundableAmount)
        {
            if (decisionAction ==
                SupportTicketDecisionAction.FullRefundRecommended)
            {
                if (requestedAmount.HasValue
                    &&
                    RoundMoney(requestedAmount.Value) !=
                        remainingRefundableAmount)
                {
                    throw new ArgumentException(
                        "For a full refund, omit RefundAmount or send the " +
                        "entire remaining refundable amount.");
                }

                return remainingRefundableAmount;
            }

            if (!requestedAmount.HasValue)
            {
                throw new ArgumentException(
                    "RefundAmount is required for a partial refund.");
            }

            var normalizedAmount =
                RoundMoney(requestedAmount.Value);

            if (normalizedAmount <= 0)
            {
                throw new ArgumentException(
                    "The partial refund amount must be greater than zero.");
            }

            if (normalizedAmount >= remainingRefundableAmount)
            {
                throw new ArgumentException(
                    "A partial refund must be less than the remaining " +
                    "refundable payment amount.");
            }

            return normalizedAmount;
        }

        private static SupportTicketDecisionStatus
            ParseCanonicalDecisionStatus(
                string? value)
        {
            if (string.IsNullOrWhiteSpace(value)
                ||
                !Enum.TryParse<SupportTicketDecisionStatus>(
                    value.Trim(),
                    ignoreCase: true,
                    out var decisionStatus)
                ||
                !Enum.IsDefined(decisionStatus))
            {
                throw new ArgumentException(
                    "The support ticket decision status is invalid.");
            }

            return decisionStatus;
        }

        private static SupportTicketDecisionAction
            ParseCanonicalDecisionAction(
                string? value)
        {
            if (string.IsNullOrWhiteSpace(value)
                ||
                !Enum.TryParse<SupportTicketDecisionAction>(
                    value.Trim(),
                    ignoreCase: true,
                    out var decisionAction)
                ||
                !Enum.IsDefined(decisionAction))
            {
                throw new ArgumentException(
                    "The support ticket decision action is invalid.");
            }

            return decisionAction;
        }

        private static bool IsPayoutHeld(
            BookingPayoutResponse? payout)
        {
            return HasPayoutStatus(
                payout,
                BookingPayoutStatus.Held);
        }

        private static bool IsPayoutBlocked(
            BookingPayoutResponse? payout)
        {
            return HasPayoutStatus(
                payout,
                BookingPayoutStatus.Blocked);
        }

        private static bool IsPayoutRefunded(
            BookingPayoutResponse? payout)
        {
            return HasPayoutStatus(
                payout,
                BookingPayoutStatus.Refunded);
        }

        private static bool IsOpenPayoutStatus(
            BookingPayoutResponse? payout)
        {
            return HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Pending)
                ||
                HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Available);
        }

        private static bool IsPayoutReleasedFromHold(
            BookingPayoutResponse? payout)
        {
            return payout is not null
                &&
                payout.ReleasedAt.HasValue
                &&
                (
                    HasPayoutStatus(
                        payout,
                        BookingPayoutStatus.Pending)
                    ||
                    HasPayoutStatus(
                        payout,
                        BookingPayoutStatus.Available)
                );
        }

        private static bool IsPayoutHoldable(
            BookingPayoutResponse? payout)
        {
            return HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Pending)
                ||
                HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Held)
                ||
                HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Available);
        }

        private static bool IsPayoutBlockable(
            BookingPayoutResponse? payout)
        {
            return HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Pending)
                ||
                HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Held)
                ||
                HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Available)
                ||
                HasPayoutStatus(
                    payout,
                    BookingPayoutStatus.Blocked);
        }

        private static bool HasPayoutStatus(
            BookingPayoutResponse? payout,
            BookingPayoutStatus expectedStatus)
        {
            return payout is not null
                &&
                string.Equals(
                    payout.Status,
                    expectedStatus.ToString(),
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasRefundStatus(
            PaymentRefundResponse refund,
            PaymentRefundStatus expectedStatus)
        {
            return string.Equals(
                refund.Status,
                expectedStatus.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task TryCreateAdminActionLogAsync(
            Guid adminUserId,
            CreateAdminActionLogRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                await _adminActionLogService.CreateAsync(
                    adminUserId,
                    request,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to create admin action log. " +
                    "ActionType: {ActionType}, " +
                    "TargetType: {TargetType}, " +
                    "TargetId: {TargetId}.",
                    request.ActionType,
                    request.TargetType,
                    request.TargetId);
            }
        }

        private static void ValidateTicketIdentifier(
            Guid ticketId)
        {
            if (ticketId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The support ticket identifier is invalid.");
            }
        }

        private static string? NormalizeOptionalText(
            string? value,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalizedValue = value.Trim();

            if (normalizedValue.Length > maximumLength)
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

        private Guid GetCurrentUserId()
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ??
                User.FindFirstValue(
                    "sub");

            if (!Guid.TryParse(
                    userIdValue,
                    out var userId)
                ||
                userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain " +
                    "a valid user identifier.");
            }

            return userId;
        }

        private string? GetClientIpAddress()
        {
            return HttpContext
                .Connection
                .RemoteIpAddress?
                .ToString();
        }

        private string? GetUserAgent()
        {
            var userAgent =
                Request.Headers["User-Agent"]
                    .ToString();

            return string.IsNullOrWhiteSpace(userAgent)
                ? null
                : userAgent;
        }

        private sealed class DecisionContext
        {
            public SupportTicketDecisionAction EffectiveAction
            { get; set; }

            public bool ShouldReleaseHeldPayoutAfterResolution
            { get; set; }
        }

        private sealed class ResolutionContext
        {
            public bool ShouldReleaseHeldPayout
            { get; set; }
        }

        private sealed class PayoutDecisionOutcome
        {
            public AdminActionType ActionType
            { get; set; }

            public BookingPayoutResponse Payout
            { get; set; } = null!;

            public string Details
            { get; set; } = string.Empty;
        }

        private sealed class RefundablePaymentSnapshot
        {
            public Guid PaymentId
            { get; init; }

            public decimal Amount
            { get; init; }

            public decimal RefundedAmount
            { get; init; }

            public string Currency
            { get; init; } = string.Empty;

            public PaymentStatus Status
            { get; init; }
        }

        private sealed class PartialRefundResolutionSnapshot
        {
            public decimal PaymentAmount
            { get; init; }

            public decimal RefundedAmount
            { get; init; }

            public PaymentStatus PaymentStatus
            { get; init; }

            public decimal BookingSubtotal
            { get; init; }

            public decimal SuccessfulRefundAmount
            { get; init; }
        }

        private sealed class PayoutRefundReconciliationOutcome
        {
            public AdminActionType ActionType
            { get; init; }

            public BookingPayoutResponse Payout
            { get; init; } = null!;

            public string Details
            { get; init; } = string.Empty;

            public string ResponseMessage
            { get; init; } = string.Empty;
        }

        private sealed class RefundExecutionContext
        {
            public SupportTicketResponse Ticket
            { get; init; } = null!;

            public SupportTicketDecisionAction DecisionAction
            { get; init; }

            public Guid BookingId
            { get; init; }

            public Guid PaymentId
            { get; init; }

            public Guid PayoutId
            { get; init; }

            public decimal PayoutAmountBeforeRefund
            { get; init; }

            public decimal RefundAmount
            { get; init; }

            public decimal RemainingRefundableAmountBeforeRefund
            { get; init; }

            public string Currency
            { get; init; } = string.Empty;

            public string? RefundNote
            { get; init; }
        }
    }
}