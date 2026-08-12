using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminDashboardController
        : ControllerBase
    {
        private readonly IAdminDashboardService
            _adminDashboardService;

        public AdminDashboardController(
            IAdminDashboardService adminDashboardService)
        {
            ArgumentNullException.ThrowIfNull(
                adminDashboardService);

            _adminDashboardService =
                adminDashboardService;
        }

        /*
         * GET:
         * /api/admin/dashboard/summary
         *
         * Returns the main statistics needed by
         * the Admin Dashboard UI.
         */
        [HttpGet("summary")]
        [ProducesResponseType(
            typeof(AdminDashboardSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<
            ActionResult<AdminDashboardSummaryResponse>>
            GetSummaryAsync(
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminDashboardService
                    .GetSummaryAsync(
                        cancellationToken);

            return Ok(response);
        }
    }
}