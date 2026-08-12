using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/verifications")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminVerificationsController
        : ControllerBase
    {
        private readonly IAdminVerificationQueueService
            _adminVerificationQueueService;

        public AdminVerificationsController(
            IAdminVerificationQueueService
                adminVerificationQueueService)
        {
            ArgumentNullException.ThrowIfNull(
                adminVerificationQueueService);

            _adminVerificationQueueService =
                adminVerificationQueueService;
        }

        /*
         * GET:
         * /api/admin/verifications/queue
         *
         * Examples:
         *
         * /api/admin/verifications/queue
         * /api/admin/verifications/queue?type=host
         * /api/admin/verifications/queue?type=property
         * /api/admin/verifications/queue?search=sarah
         */
        [HttpGet("queue")]
        [ProducesResponseType(
            typeof(AdminVerificationQueueResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminVerificationQueueResponse>>
            GetQueueAsync(
                [FromQuery] AdminVerificationQueueRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminVerificationQueueService
                    .GetQueueAsync(
                        request,
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * GET:
         * /api/admin/verifications/{verificationType}/{verificationId}/history
         *
         * Examples:
         *
         * /api/admin/verifications/host/{id}/history
         * /api/admin/verifications/property/{id}/history
         */
        [HttpGet("{verificationType}/{verificationId:guid}/history")]
        [ProducesResponseType(
            typeof(AdminVerificationHistoryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminVerificationHistoryResponse>>
            GetHistoryAsync(
                string verificationType,
                Guid verificationId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminVerificationQueueService
                    .GetHistoryAsync(
                        verificationType,
                        verificationId,
                        cancellationToken);

            return Ok(
                response);
        }
    }
}