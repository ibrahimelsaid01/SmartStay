using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/properties")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminPropertiesController
        : ControllerBase
    {
        private readonly IAdminPropertyService
            _adminPropertyService;

        public AdminPropertiesController(
            IAdminPropertyService adminPropertyService)
        {
            ArgumentNullException.ThrowIfNull(
                adminPropertyService);

            _adminPropertyService =
                adminPropertyService;
        }

        /*
         * GET:
         * /api/admin/properties/pending?page=1&pageSize=20
         */
        [HttpGet("pending")]
        [ProducesResponseType(
            typeof(AdminPendingPropertiesResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<
            ActionResult<AdminPendingPropertiesResponse>>
            GetPendingAsync(
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                CancellationToken cancellationToken =
                    default)
        {
            var response =
                await _adminPropertyService
                    .GetPendingAsync(
                        page,
                        pageSize,
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * GET:
         * /api/admin/properties/{propertyId}
         */
        [HttpGet("{propertyId:guid}")]
        [ProducesResponseType(
            typeof(AdminPropertyDetailsResponse),
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
            ActionResult<AdminPropertyDetailsResponse>>
            GetByIdAsync(
                Guid propertyId,
                CancellationToken cancellationToken =
                    default)
        {
            var response =
                await _adminPropertyService
                    .GetByIdAsync(
                        propertyId,
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * GET:
         * /api/admin/properties/{propertyId}/
         * verification-document/pages/{pageId}/content
         */
        [HttpGet(
            "{propertyId:guid}/verification-document/" +
            "pages/{pageId:guid}/content")]
        [ProducesResponseType(
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult>
            GetVerificationDocumentPageContentAsync(
                Guid propertyId,
                Guid pageId,
                CancellationToken cancellationToken =
                    default)
        {
            var imageContent =
                await _adminPropertyService
                    .GetVerificationDocumentPageContentAsync(
                        propertyId,
                        pageId,
                        cancellationToken);

            return File(
                imageContent.Content,
                imageContent.ContentType);
        }

        /*
         * POST:
         * /api/admin/properties/{propertyId}/approve
         */
        [HttpPost("{propertyId:guid}/approve")]
        [ProducesResponseType(
            typeof(AdminPropertyReviewResponse),
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
            ActionResult<AdminPropertyReviewResponse>>
            ApproveAsync(
                Guid propertyId,
                CancellationToken cancellationToken =
                    default)
        {
            var response =
                await _adminPropertyService
                    .ApproveAsync(
                        propertyId,
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * POST:
         * /api/admin/properties/{propertyId}/reject
         */
        [HttpPost("{propertyId:guid}/reject")]
        [ProducesResponseType(
            typeof(AdminPropertyReviewResponse),
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
            ActionResult<AdminPropertyReviewResponse>>
            RejectAsync(
                Guid propertyId,
                [FromBody]
                RejectPropertyRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var response =
                await _adminPropertyService
                    .RejectAsync(
                        propertyId,
                        request.Reason
                        ?? string.Empty,
                        cancellationToken);

            return Ok(
                response);
        }
    }
}