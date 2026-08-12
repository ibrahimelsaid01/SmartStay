using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/bookings")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminBookingsController
        : ControllerBase
    {
        private readonly IAdminBookingService
            _adminBookingService;

        public AdminBookingsController(
            IAdminBookingService adminBookingService)
        {
            ArgumentNullException.ThrowIfNull(
                adminBookingService);

            _adminBookingService =
                adminBookingService;
        }

        /*
         * GET:
         * /api/admin/bookings
         *
         * Examples:
         *
         * /api/admin/bookings?page=1&pageSize=20
         *
         * /api/admin/bookings?status=Confirmed
         *
         * /api/admin/bookings
         * ?hostUserId=HOST_USER_ID
         * &checkInFrom=2026-08-01
         * &checkInTo=2026-08-31
         * &page=1
         * &pageSize=20
         */
        [HttpGet]
        [ProducesResponseType(
            typeof(AdminBookingsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminBookingsResponse>>
            GetBookingsAsync(
                [FromQuery] AdminBookingSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminBookingService
                    .GetBookingsAsync(
                        request,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/admin/bookings/summary
         *
         * Returns booking counts and financial
         * snapshots grouped by currency.
         */
        [HttpGet("summary")]
        [ProducesResponseType(
            typeof(AdminBookingSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<
            ActionResult<AdminBookingSummaryResponse>>
            GetSummaryAsync(
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminBookingService
                    .GetSummaryAsync(
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/admin/bookings/{bookingId}
         *
         * Returns full administrative booking details.
         */
        [HttpGet("{bookingId:guid}")]
        [ProducesResponseType(
            typeof(AdminBookingDetailsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<AdminBookingDetailsResponse>>
            GetBookingByIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminBookingService
                    .GetBookingByIdAsync(
                        bookingId,
                        cancellationToken);

            return Ok(response);
        }
    }
}