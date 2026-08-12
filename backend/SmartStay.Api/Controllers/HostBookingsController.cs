using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/host/bookings")]
    [Authorize(Roles = RoleNames.Host)]
    public sealed class HostBookingsController
        : ControllerBase
    {
        private readonly IHostBookingService
            _hostBookingService;

        public HostBookingsController(
            IHostBookingService hostBookingService)
        {
            ArgumentNullException.ThrowIfNull(
                hostBookingService);

            _hostBookingService =
                hostBookingService;
        }

        /*
         * GET:
         * /api/host/bookings
         *
         * Examples:
         *
         * /api/host/bookings?page=1&pageSize=10
         *
         * /api/host/bookings
         * ?status=Confirmed
         * &page=1
         * &pageSize=10
         */
        [HttpGet]
        [ProducesResponseType(
            typeof(HostBookingsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<HostBookingsResponse>>
            GetBookingsAsync(
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] BookingStatus? status = null,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostBookingService
                    .GetBookingsAsync(
                        GetAuthenticatedUserId(),
                        page,
                        pageSize,
                        status,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/host/bookings/summary
         */
        [HttpGet("summary")]
        [ProducesResponseType(
            typeof(HostBookingSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<
            ActionResult<HostBookingSummaryResponse>>
            GetSummaryAsync(
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostBookingService
                    .GetSummaryAsync(
                        GetAuthenticatedUserId(),
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/host/bookings/{bookingId}
         *
         * The booking is returned only when
         * its property belongs to the current host.
         */
        [HttpGet("{bookingId:guid}")]
        [ProducesResponseType(
            typeof(HostBookingDetailsResponse),
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
        public async Task<
            ActionResult<HostBookingDetailsResponse>>
            GetBookingByIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostBookingService
                    .GetBookingByIdAsync(
                        GetAuthenticatedUserId(),
                        bookingId,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * The authenticated user identifier is extracted
         * from the JWT access token.
         *
         * The host user ID is never accepted from the
         * query string, route or request body.
         */
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