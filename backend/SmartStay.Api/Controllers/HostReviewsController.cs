using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/host/reviews")]
    [Authorize(Roles = RoleNames.Host)]
    public sealed class HostReviewsController
        : ControllerBase
    {
        private readonly IHostReviewService
            _hostReviewService;

        public HostReviewsController(
            IHostReviewService hostReviewService)
        {
            ArgumentNullException.ThrowIfNull(
                hostReviewService);

            _hostReviewService =
                hostReviewService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(HostReviewsResponse),
            StatusCodes.Status200OK)]
        public async Task<
            ActionResult<HostReviewsResponse>>
            GetReviewsAsync(
                [FromQuery] Guid? propertyId = null,
                [FromQuery] bool unansweredOnly = false,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostReviewService
                    .GetReviewsAsync(
                        GetAuthenticatedUserId(),
                        propertyId,
                        unansweredOnly,
                        page,
                        pageSize,
                        cancellationToken);

            return Ok(response);
        }

        [HttpGet("{reviewId:guid}")]
        [ProducesResponseType(
            typeof(HostReviewResponse),
            StatusCodes.Status200OK)]
        public async Task<
            ActionResult<HostReviewResponse>>
            GetByIdAsync(
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostReviewService
                    .GetByIdAsync(
                        GetAuthenticatedUserId(),
                        reviewId,
                        cancellationToken);

            return Ok(response);
        }

        [HttpPost("{reviewId:guid}/reply")]
        [ProducesResponseType(
            typeof(HostReviewResponse),
            StatusCodes.Status201Created)]
        public async Task<
            ActionResult<HostReviewResponse>>
            CreateReplyAsync(
                Guid reviewId,
                [FromBody]
                UpsertReviewReplyRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostReviewService
                    .CreateReplyAsync(
                        GetAuthenticatedUserId(),
                        reviewId,
                        request,
                        cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }

        [HttpPut("{reviewId:guid}/reply")]
        [ProducesResponseType(
            typeof(HostReviewResponse),
            StatusCodes.Status200OK)]
        public async Task<
            ActionResult<HostReviewResponse>>
            UpdateReplyAsync(
                Guid reviewId,
                [FromBody]
                UpsertReviewReplyRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _hostReviewService
                    .UpdateReplyAsync(
                        GetAuthenticatedUserId(),
                        reviewId,
                        request,
                        cancellationToken);

            return Ok(response);
        }

        [HttpDelete("{reviewId:guid}/reply")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        public async Task<IActionResult>
            DeleteReplyAsync(
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            await _hostReviewService
                .DeleteReplyAsync(
                    GetAuthenticatedUserId(),
                    reviewId,
                    cancellationToken);

            return NoContent();
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