using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public sealed class BookingsController
        : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(
            IBookingService bookingService)
        {
            ArgumentNullException.ThrowIfNull(
                bookingService);

            _bookingService = bookingService;
        }

        /*
         * POST:
         * /api/bookings
         *
         * Creates a new booking for the authenticated user.
         */
        [HttpPost]
        [ProducesResponseType(
            typeof(CreateBookingResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CreateBookingResponse>>
            CreateAsync(
                [FromBody] CreateBookingRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _bookingService.CreateAsync(
                    GetAuthenticatedUserId(),
                    request,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }

        /*
         * GET:
         * /api/bookings/my-bookings
         */
        [HttpGet("my-bookings")]
        [ProducesResponseType(
            typeof(GuestBookingsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GuestBookingsResponse>>
            GetMyBookingsAsync(
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] BookingStatus? status = null,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _bookingService
                    .GetGuestBookingsAsync(
                        GetAuthenticatedUserId(),
                        page,
                        pageSize,
                        status,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/bookings/{bookingId}
         */
        [HttpGet("{bookingId:guid}")]
        [ProducesResponseType(
            typeof(GuestBookingDetailsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<GuestBookingDetailsResponse>>
            GetByIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _bookingService
                    .GetGuestBookingByIdAsync(
                        GetAuthenticatedUserId(),
                        bookingId,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/bookings/{bookingId}/confirmation
         *
         * Returns the data required by the booking
         * success page after successful payment.
         */
        [HttpGet("{bookingId:guid}/confirmation")]
        [ProducesResponseType(
            typeof(GuestBookingConfirmationResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<
            ActionResult<GuestBookingConfirmationResponse>>
            GetConfirmationAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _bookingService
                    .GetGuestBookingConfirmationAsync(
                        GetAuthenticatedUserId(),
                        bookingId,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * POST:
         * /api/bookings/{bookingId}/cancel
         */
        [HttpPost("{bookingId:guid}/cancel")]
        [ProducesResponseType(
            typeof(CancelBookingResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CancelBookingResponse>>
            CancelAsync(
                Guid bookingId,
                [FromBody] CancelBookingRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _bookingService
                    .CancelGuestBookingAsync(
                        GetAuthenticatedUserId(),
                        bookingId,
                        request,
                        cancellationToken);

            return Ok(response);
        }

        private Guid GetAuthenticatedUserId()
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(
                    userIdValue,
                    out var userId))
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain a valid user identifier.");
            }

            return userId;
        }
    }
}