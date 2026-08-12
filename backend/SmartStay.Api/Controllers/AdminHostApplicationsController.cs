using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/host-applications")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminHostApplicationsController
        : ControllerBase
    {
        private readonly IAdminHostApplicationService
            _adminHostApplicationService;

        public AdminHostApplicationsController(
            IAdminHostApplicationService
                adminHostApplicationService)
        {
            _adminHostApplicationService =
                adminHostApplicationService;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<
            IReadOnlyList<
                AdminHostApplicationSummaryResponse>>>
            GetPendingAsync(
                CancellationToken cancellationToken)
        {
            var applications =
                await _adminHostApplicationService
                    .GetPendingAsync(
                        cancellationToken);

            return Ok(applications);
        }





        [HttpGet("{id:guid}")]
        public async Task<
    ActionResult<AdminHostApplicationDetailsResponse>>
    GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
        {
            var application =
                await _adminHostApplicationService
                    .GetByIdAsync(
                        id,
                        cancellationToken);

            return Ok(application);
        }



        [HttpGet(
    "{id:guid}/identity-document/front")]
        [Produces(
    "image/jpeg",
    "image/png",
    "image/webp")]
        public async Task<IActionResult>
    GetIdentityDocumentFrontAsync(
        Guid id,
        CancellationToken cancellationToken)
        {
            var image =
                await _adminHostApplicationService
                    .GetIdentityDocumentImageAsync(
                        id,
                        HostIdentityDocumentSide.Front,
                        cancellationToken);

            return CreateSensitiveImageResult(
                image);
        }




        private FileContentResult CreateSensitiveImageResult(
    ImageContentResult image)
        {
            Response.Headers.CacheControl =
                "no-store, no-cache, max-age=0";

            Response.Headers.Pragma =
                "no-cache";

            Response.Headers[
                "X-Content-Type-Options"] =
                    "nosniff";

            return File(
                image.Content,
                image.ContentType);
        }



        [HttpPost("{id:guid}/approve")]
        public async Task<
    ActionResult<AdminHostApplicationDetailsResponse>> ApproveAsync(Guid id, CancellationToken cancellationToken)
        {
            var application =
                await _adminHostApplicationService
                    .ApproveAsync(
                        id,
                        cancellationToken);

            return Ok(application);
        }




        [HttpPost("{id:guid}/reject")]
        public async Task<
    ActionResult<AdminHostApplicationDetailsResponse>> RejectAsync(Guid id,
        [FromBody]
        RejectHostApplicationRequest request,
        CancellationToken cancellationToken)
        {
            var application =
                await _adminHostApplicationService
                    .RejectAsync(
                        id,
                        request,
                        cancellationToken);

            return Ok(application);
        }

    }
}