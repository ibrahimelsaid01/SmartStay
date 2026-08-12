using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/payments")]
    [Authorize(Roles = "User")]
    public sealed class PaymentsController
        : ControllerBase
    {
        private readonly IPaymentService
            _paymentService;

        public PaymentsController(
            IPaymentService paymentService)
        {
            ArgumentNullException.ThrowIfNull(
                paymentService);

            _paymentService =
                paymentService;
        }

        // =====================================================
        // Start Stripe payment
        // =====================================================

        /*
         * POST /api/payments
         *
         * Headers:
         *
         * Authorization: Bearer {token}
         * Idempotency-Key: {unique-key}
         *
         * Body:
         *
         * {
         *   "bookingId": "..."
         * }
         */
        [HttpPost]
        [ProducesResponseType(
            typeof(StartPaymentResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(StartPaymentResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status409Conflict)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<StartPaymentResponse>>
            StartPaymentAsync(
                [FromBody]
                StartPaymentRequest request,

                [FromHeader(Name = "Idempotency-Key")]
                string? idempotencyKey,

                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                    idempotencyKey))
            {
                ModelState.AddModelError(
                    "Idempotency-Key",
                    "The Idempotency-Key header is required.");

                return ValidationProblem(
                    ModelState);
            }

            var guestUserId =
                GetAuthenticatedUserId();

            var response =
                await _paymentService
                    .StartPaymentAsync(
                        guestUserId,
                        request,
                        idempotencyKey,
                        cancellationToken);

            /*
             * Repeating the same:
             *
             * BookingId + Idempotency-Key
             *
             * returns the existing payment using HTTP 200.
             */
            if (response.WasAlreadyProcessed)
            {
                return Ok(
                    response);
            }

            /*
             * A new local payment and Stripe PaymentIntent
             * were created.
             *
             * We return 201 directly instead of CreatedAtAction
             * because route generation can fail while building
             * the Location header, even though the payment was
             * created successfully.
             */
            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }

        // =====================================================
        // Get local payment status
        // =====================================================

        /*
         * GET /api/payments/{paymentId}
         */
        [HttpGet("{paymentId:guid}")]
        [ProducesResponseType(
            typeof(PaymentStatusResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaymentStatusResponse>>
            GetPaymentStatusAsync(
                [FromRoute]
                Guid paymentId,

                CancellationToken cancellationToken)
        {
            var guestUserId =
                GetAuthenticatedUserId();

            var response =
                await _paymentService
                    .GetPaymentStatusAsync(
                        guestUserId,
                        paymentId,
                        cancellationToken);

            return Ok(
                response);
        }

        // =====================================================
        // Authenticated user identifier
        // =====================================================

        private Guid GetAuthenticatedUserId()
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ??
                User.FindFirstValue(
                    JwtRegisteredClaimNames.Sub);

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
    }
}