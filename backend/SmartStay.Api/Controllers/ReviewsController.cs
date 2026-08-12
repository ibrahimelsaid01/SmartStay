using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/reviews")]
    [Authorize]
    public sealed class ReviewsController
        : ControllerBase
    {
        private const string GetMyReviewByIdRouteName =
            "GetMyReviewById";

        private readonly IReviewService _reviewService;

        public ReviewsController(
            IReviewService reviewService)
        {
            ArgumentNullException.ThrowIfNull(
                reviewService);

            _reviewService = reviewService;
        }

        /*
         * GET:
         * /api/reviews/public/featured?limit=3
         *
         * Public Home-page reviews.
         * Returns only reviews that are eligible for
         * public display according to the review service.
         */
        [AllowAnonymous]
        [HttpGet("public/featured")]
        [ProducesResponseType(
            typeof(IReadOnlyList<FeaturedReviewResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<
            IReadOnlyList<FeaturedReviewResponse>>>
            GetFeaturedReviewsAsync(
                [FromQuery] int limit = 3,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService
                    .GetFeaturedReviewsAsync(
                        limit,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * POST:
         * /api/bookings/{bookingId}/review
         */
        [HttpPost(
            "~/api/bookings/{bookingId:guid}/review")]
        [ProducesResponseType(
            typeof(UserReviewResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserReviewResponse>>
            CreateAsync(
                Guid bookingId,
                [FromBody] CreateReviewRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService.CreateAsync(
                    GetAuthenticatedUserId(),
                    bookingId,
                    request,
                    cancellationToken);

            /*
             * Use an explicitly named route instead of relying on the
             * action method name. ASP.NET Core may suppress the Async
             * suffix when generating action names, which previously
             * caused CreatedAtAction to fail after the review had
             * already been saved.
             */
            return CreatedAtRoute(
                GetMyReviewByIdRouteName,
                new
                {
                    reviewId = response.Id
                },
                response);
        }

        /*
         * GET:
         * /api/reviews/my-reviews
         *
         * Examples:
         *
         * /api/reviews/my-reviews
         *
         * /api/reviews/my-reviews
         * ?status=Pending&page=1&pageSize=10
         */
        [HttpGet("my-reviews")]
        [ProducesResponseType(
            typeof(MyReviewsResponse),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<MyReviewsResponse>>
            GetMyReviewsAsync(
                [FromQuery] ReviewStatus? status = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService.GetMyReviewsAsync(
                    GetAuthenticatedUserId(),
                    status,
                    page,
                    pageSize,
                    cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/reviews/{reviewId}
         *
         * Returns the review only when it belongs
         * to the authenticated user.
         */
        [HttpGet(
            "{reviewId:guid}",
            Name = GetMyReviewByIdRouteName)]
        [ProducesResponseType(
            typeof(UserReviewResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserReviewResponse>>
            GetByIdAsync(
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService
                    .GetMyReviewByIdAsync(
                        GetAuthenticatedUserId(),
                        reviewId,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * PUT:
         * /api/reviews/{reviewId}
         *
         * Pending reviews can be edited.
         * Rejected reviews are resubmitted as Pending.
         */
        [HttpPut("{reviewId:guid}")]
        [ProducesResponseType(
            typeof(UserReviewResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserReviewResponse>>
            UpdateAsync(
                Guid reviewId,
                [FromBody] UpdateReviewRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService.UpdateAsync(
                    GetAuthenticatedUserId(),
                    reviewId,
                    request,
                    cancellationToken);

            return Ok(response);
        }

        /*
         * DELETE:
         * /api/reviews/{reviewId}
         */
        [HttpDelete("{reviewId:guid}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            await _reviewService.DeleteAsync(
                GetAuthenticatedUserId(),
                reviewId,
                cancellationToken);

            return NoContent();
        }

        /*
         * POST:
         * /api/reviews/{reviewId}/helpful
         */
        [HttpPost("{reviewId:guid}/helpful")]
        [ProducesResponseType(
            typeof(ReviewHelpfulResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ReviewHelpfulResponse>>
            MarkHelpfulAsync(
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService.MarkHelpfulAsync(
                    GetAuthenticatedUserId(),
                    reviewId,
                    cancellationToken);

            return Ok(response);
        }

        /*
         * DELETE:
         * /api/reviews/{reviewId}/helpful
         */
        [HttpDelete("{reviewId:guid}/helpful")]
        [ProducesResponseType(
            typeof(ReviewHelpfulResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReviewHelpfulResponse>>
            RemoveHelpfulAsync(
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService.RemoveHelpfulAsync(
                    GetAuthenticatedUserId(),
                    reviewId,
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