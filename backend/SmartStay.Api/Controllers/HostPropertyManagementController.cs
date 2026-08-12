using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/host/properties")]
    [Authorize(Roles = RoleNames.Host)]
    public sealed class HostPropertyManagementController
        : ControllerBase
    {
        private readonly IHostPropertyManagementService
            _hostPropertyManagementService;

        public HostPropertyManagementController(
            IHostPropertyManagementService
                hostPropertyManagementService)
        {
            ArgumentNullException.ThrowIfNull(
                hostPropertyManagementService);

            _hostPropertyManagementService =
                hostPropertyManagementService;
        }

        /*
         * GET:
         * /api/host/properties
         *
         * Examples:
         * /api/host/properties?page=1&pageSize=10
         * /api/host/properties?status=Draft
         * /api/host/properties?status=Published
         */
        [HttpGet]
        [ProducesResponseType(
            typeof(HostPropertiesResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<
            ActionResult<HostPropertiesResponse>>
            GetPropertiesAsync(
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] PropertyStatus? status = null,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostPropertyManagementService
                    .GetPropertiesAsync(
                        GetAuthenticatedUserId(),
                        page,
                        pageSize,
                        status,
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * GET:
         * /api/host/properties/summary
         */
        [HttpGet("summary")]
        [ProducesResponseType(
            typeof(HostPropertyStatusSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<
            ActionResult<
                HostPropertyStatusSummaryResponse>>
            GetSummaryAsync(
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostPropertyManagementService
                    .GetSummaryAsync(
                        GetAuthenticatedUserId(),
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * POST:
         * /api/host/properties/{propertyId}/unpublish
         */
        [HttpPost("{propertyId:guid}/unpublish")]
        [ProducesResponseType(
            typeof(HostPropertyUnpublishResponse),
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
            ActionResult<HostPropertyUnpublishResponse>>
            UnpublishAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostPropertyManagementService
                    .UnpublishAsync(
                        GetAuthenticatedUserId(),
                        propertyId,
                        cancellationToken);

            return Ok(
                response);
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