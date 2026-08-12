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
    [Route("api/admin/booking-payouts")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminBookingPayoutsController
        : ControllerBase
    {
        private const int MaximumNoteLength =
            1000;

        private readonly IBookingPayoutService
            _bookingPayoutService;

        private readonly IAdminActionLogService
            _adminActionLogService;

        private readonly SmartStayDbContext
            _dbContext;

        private readonly ILogger<AdminBookingPayoutsController>
            _logger;

        public AdminBookingPayoutsController(
            IBookingPayoutService bookingPayoutService,
            IAdminActionLogService adminActionLogService,
            SmartStayDbContext dbContext,
            ILogger<AdminBookingPayoutsController> logger)
        {
            ArgumentNullException.ThrowIfNull(
                bookingPayoutService);

            ArgumentNullException.ThrowIfNull(
                adminActionLogService);

            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                logger);

            _bookingPayoutService =
                bookingPayoutService;

            _adminActionLogService =
                adminActionLogService;

            _dbContext =
                dbContext;

            _logger =
                logger;
        }

        [HttpGet("bookings/{bookingId:guid}")]
        [ProducesResponseType(
            typeof(BookingPayoutResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingPayoutResponse>>
            GetByBookingIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            ValidateBookingIdentifier(
                bookingId);

            var response =
                await _bookingPayoutService
                    .GetByBookingIdAsync(
                        bookingId,
                        cancellationToken);

            if (response is null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "The booking payout was not found."
                    });
            }

            return Ok(
                response);
        }

        [HttpPatch("bookings/{bookingId:guid}/hold")]
        [ProducesResponseType(
            typeof(BookingPayoutResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingPayoutResponse>>
            HoldAsync(
                Guid bookingId,
                HoldBookingPayoutRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateBookingIdentifier(
                bookingId);

            ArgumentNullException.ThrowIfNull(
                request);

            request.Reason =
                NormalizeRequiredText(
                    request.Reason,
                    "The payout hold reason is required.",
                    MaximumNoteLength);

            var adminUserId =
                GetCurrentUserId();

            var response =
                await _bookingPayoutService
                    .HoldPayoutForBookingAsync(
                        bookingId,
                        request.Reason,
                        cancellationToken);

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.PayoutHeld.ToString(),

                    TargetType =
                        AdminActionTargetType.Payout.ToString(),

                    TargetId =
                        response.PayoutId,

                    TargetReference =
                        response.BookingId.ToString(),

                    Summary =
                        $"Admin held payout {response.PayoutId} " +
                        $"for booking {response.BookingId}.",

                    Details =
                        request.Reason,

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            return Ok(
                response);
        }

        [HttpPatch("bookings/{bookingId:guid}/release")]
        [ProducesResponseType(
            typeof(BookingPayoutResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingPayoutResponse>>
            ReleaseAsync(
                Guid bookingId,
                ReleaseBookingPayoutRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateBookingIdentifier(
                bookingId);

            ArgumentNullException.ThrowIfNull(
                request);

            request.ReleaseNote =
                NormalizeOptionalText(
                    request.ReleaseNote,
                    MaximumNoteLength);

            var adminUserId =
                GetCurrentUserId();

            var response =
                await _bookingPayoutService
                    .ReleasePayoutForBookingAsync(
                        bookingId,
                        request.ReleaseNote,
                        cancellationToken);

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.PayoutReleased.ToString(),

                    TargetType =
                        AdminActionTargetType.Payout.ToString(),

                    TargetId =
                        response.PayoutId,

                    TargetReference =
                        response.BookingId.ToString(),

                    Summary =
                        $"Admin released payout {response.PayoutId} " +
                        $"for booking {response.BookingId}.",

                    Details =
                        request.ReleaseNote,

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            return Ok(
                response);
        }

        [HttpPatch("bookings/{bookingId:guid}/block")]
        [ProducesResponseType(
            typeof(BookingPayoutResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingPayoutResponse>>
            BlockAsync(
                Guid bookingId,
                BlockBookingPayoutRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateBookingIdentifier(
                bookingId);

            ArgumentNullException.ThrowIfNull(
                request);

            request.Reason =
                NormalizeRequiredText(
                    request.Reason,
                    "The payout block reason is required.",
                    MaximumNoteLength);

            var adminUserId =
                GetCurrentUserId();

            var response =
                await _bookingPayoutService
                    .BlockPayoutForBookingAsync(
                        bookingId,
                        request.Reason,
                        cancellationToken);

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.PayoutBlocked.ToString(),

                    TargetType =
                        AdminActionTargetType.Payout.ToString(),

                    TargetId =
                        response.PayoutId,

                    TargetReference =
                        response.BookingId.ToString(),

                    Summary =
                        $"Admin blocked payout {response.PayoutId} " +
                        $"for booking {response.BookingId}.",

                    Details =
                        request.Reason,

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            return Ok(
                response);
        }

        [HttpPatch("bookings/{bookingId:guid}/refunded")]
        [ProducesResponseType(
            typeof(BookingPayoutResponse),
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
        public async Task<ActionResult<BookingPayoutResponse>>
            MarkRefundedAsync(
                Guid bookingId,
                MarkBookingPayoutRefundedRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateBookingIdentifier(
                bookingId);

            ArgumentNullException.ThrowIfNull(
                request);

            request.RefundNote =
                NormalizeOptionalText(
                    request.RefundNote,
                    MaximumNoteLength);

            var adminUserId =
                GetCurrentUserId();

            var currentPayout =
                await _bookingPayoutService
                    .GetByBookingIdAsync(
                        bookingId,
                        cancellationToken);

            if (currentPayout is null)
            {
                throw new KeyNotFoundException(
                    "The booking payout was not found.");
            }

            var refundVerification =
                await VerifyCompletedFullRefundAsync(
                    bookingId,
                    currentPayout.BookingPaymentId,
                    cancellationToken);

            /*
             * This endpoint does not create a Stripe refund.
             *
             * Stripe and its webhook must complete the full
             * payment refund first. This operation only
             * reconciles the host payout after that verified
             * provider result exists in BookingPayment.
             */
            var response =
                await _bookingPayoutService
                    .MarkPayoutRefundedForBookingAsync(
                        bookingId,
                        request.RefundNote,
                        cancellationToken);

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType
                            .PayoutMarkedRefunded
                            .ToString(),

                    TargetType =
                        AdminActionTargetType
                            .Payout
                            .ToString(),

                    TargetId =
                        response.PayoutId,

                    TargetReference =
                        response.BookingId.ToString(),

                    Summary =
                        $"Admin reconciled payout {response.PayoutId} " +
                        $"as fully refunded for booking " +
                        $"{response.BookingId}.",

                    Details =
                        BuildRefundAuditDetails(
                            refundVerification,
                            request.RefundNote),

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            return Ok(
                response);
        }

        private async Task<FullRefundVerification>
            VerifyCompletedFullRefundAsync(
                Guid bookingId,
                Guid bookingPaymentId,
                CancellationToken cancellationToken)
        {
            var payment =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == bookingPaymentId
                        &&
                        item.BookingId == bookingId)
                    .Select(item =>
                        new
                        {
                            item.Id,
                            item.Amount,
                            item.Currency,
                            item.Status,
                            item.RefundedAmount,
                            item.RefundedAt,

                            SuccessfulRefundCount =
                                item.Refunds.Count(refund =>
                                    refund.Status ==
                                    PaymentRefundStatus
                                        .Succeeded),

                            SuccessfulRefundAmount =
                                item.Refunds
                                    .Where(refund =>
                                        refund.Status ==
                                        PaymentRefundStatus
                                            .Succeeded)
                                    .Select(refund =>
                                        (decimal?)refund.Amount)
                                    .Sum()
                                ??
                                0m
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (payment is null)
            {
                throw new KeyNotFoundException(
                    "The booking payment linked to this payout was not found.");
            }

            if (payment.Status ==
                PaymentStatus.PartiallyRefunded)
            {
                throw new InvalidOperationException(
                    "A partially refunded payment cannot mark the entire payout as refunded. " +
                    "Keep the payout blocked until the remaining host payout amount is reconciled.");
            }

            if (payment.Status !=
                PaymentStatus.Refunded)
            {
                throw new InvalidOperationException(
                    "The payout cannot be marked as refunded because the payment provider has not confirmed a full refund.");
            }

            if (!payment.RefundedAt.HasValue)
            {
                throw new InvalidOperationException(
                    "The full refund confirmation timestamp is missing from the booking payment.");
            }

            if (payment.Amount <= 0)
            {
                throw new InvalidOperationException(
                    "The original booking payment amount is invalid.");
            }

            if (payment.RefundedAmount !=
                payment.Amount)
            {
                throw new InvalidOperationException(
                    "The booking payment is not fully refunded because its refunded amount does not equal the original payment amount.");
            }

            if (payment.SuccessfulRefundCount <= 0)
            {
                throw new InvalidOperationException(
                    "No successful provider refund record exists for this booking payment.");
            }

            if (payment.SuccessfulRefundAmount !=
                payment.Amount)
            {
                throw new InvalidOperationException(
                    "The successful provider refund records do not equal the full booking payment amount.");
            }

            return new FullRefundVerification
            {
                BookingPaymentId =
                    payment.Id,

                PaymentAmount =
                    payment.Amount,

                RefundedAmount =
                    payment.RefundedAmount,

                Currency =
                    payment.Currency,

                RefundedAt =
                    payment.RefundedAt.Value,

                SuccessfulRefundCount =
                    payment.SuccessfulRefundCount
            };
        }

        private static string BuildRefundAuditDetails(
            FullRefundVerification verification,
            string? refundNote)
        {
            var details =
                $"BookingPaymentId: " +
                $"{verification.BookingPaymentId}; " +
                $"PaymentAmount: " +
                $"{verification.PaymentAmount:0.00} " +
                $"{verification.Currency}; " +
                $"RefundedAmount: " +
                $"{verification.RefundedAmount:0.00} " +
                $"{verification.Currency}; " +
                $"SuccessfulRefundOperations: " +
                $"{verification.SuccessfulRefundCount}; " +
                $"RefundedAt: " +
                $"{verification.RefundedAt:O}.";

            if (!string.IsNullOrWhiteSpace(
                    refundNote))
            {
                details +=
                    $" AdminNote: {refundNote}";
            }

            return details;
        }

        private async Task TryCreateAdminActionLogAsync(
            Guid adminUserId,
            CreateAdminActionLogRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                await _adminActionLogService
                    .CreateAsync(
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

        private static void ValidateBookingIdentifier(
            Guid bookingId)
        {
            if (bookingId ==
                Guid.Empty)
            {
                throw new ArgumentException(
                    "The booking identifier is invalid.");
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
                    $"The value cannot exceed " +
                    $"{maximumLength} characters.");
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
                    $"The value cannot exceed " +
                    $"{maximumLength} characters.");
            }

            return normalizedValue;
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

            return string.IsNullOrWhiteSpace(
                    userAgent)
                ? null
                : userAgent;
        }

        private sealed class FullRefundVerification
        {
            public Guid BookingPaymentId
            { get; set; }

            public decimal PaymentAmount
            { get; set; }

            public decimal RefundedAmount
            { get; set; }

            public string Currency
            { get; set; } =
                string.Empty;

            public DateTimeOffset RefundedAt
            { get; set; }

            public int SuccessfulRefundCount
            { get; set; }
        }
    }
}