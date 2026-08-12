using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/reviews")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminReviewsController
        : ControllerBase
    {
        private readonly IAdminReviewService
            _adminReviewService;

        public AdminReviewsController(
            IAdminReviewService adminReviewService)
        {
            ArgumentNullException.ThrowIfNull(
                adminReviewService);

            _adminReviewService =
                adminReviewService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(AdminReviewsResponse),
            StatusCodes.Status200OK)]
        public async Task<
            ActionResult<AdminReviewsResponse>>
            GetReviewsAsync(
                [FromQuery]
                ReviewStatus? status = ReviewStatus.Pending,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminReviewService
                    .GetReviewsAsync(
                        status,
                        page,
                        pageSize,
                        cancellationToken);

            return Ok(response);
        }

        [HttpGet("{reviewId:guid}")]
        [ProducesResponseType(
            typeof(AdminReviewDetailsResponse),
            StatusCodes.Status200OK)]
        public async Task<
            ActionResult<AdminReviewDetailsResponse>>
            GetByIdAsync(
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminReviewService
                    .GetByIdAsync(
                        reviewId,
                        cancellationToken);

            return Ok(response);
        }

        [HttpPost("{reviewId:guid}/approve")]
        [ProducesResponseType(
            typeof(AdminReviewModerationResponse),
            StatusCodes.Status200OK)]
        public async Task<
            ActionResult<AdminReviewModerationResponse>>
            ApproveAsync(
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminReviewService
                    .ApproveAsync(
                        GetAuthenticatedUserId(),
                        reviewId,
                        cancellationToken);

            return Ok(response);
        }

        [HttpPost("{reviewId:guid}/reject")]
        [ProducesResponseType(
            typeof(AdminReviewModerationResponse),
            StatusCodes.Status200OK)]
        public async Task<
            ActionResult<AdminReviewModerationResponse>>
            RejectAsync(
                Guid reviewId,
                [FromBody]
                RejectReviewRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminReviewService
                    .RejectAsync(
                        GetAuthenticatedUserId(),
                        reviewId,
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