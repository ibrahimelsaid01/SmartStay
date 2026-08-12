using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/action-logs")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminActionLogsController
        : ControllerBase
    {
        private readonly IAdminActionLogService
            _adminActionLogService;

        public AdminActionLogsController(
            IAdminActionLogService adminActionLogService)
        {
            ArgumentNullException.ThrowIfNull(
                adminActionLogService);

            _adminActionLogService =
                adminActionLogService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(AdminActionLogsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminActionLogsResponse>>
            GetLogsAsync(
                [FromQuery] AdminActionLogSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminActionLogService
                    .GetLogsAsync(
                        request,
                        cancellationToken);

            return Ok(
                response);
        }

        [HttpGet("{logId:guid}")]
        [ProducesResponseType(
            typeof(AdminActionLogResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminActionLogResponse>>
            GetByIdAsync(
                Guid logId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminActionLogService
                    .GetByIdAsync(
                        logId,
                        cancellationToken);

            return Ok(
                response);
        }
    }
}