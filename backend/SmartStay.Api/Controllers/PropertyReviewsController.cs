using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/properties/{propertyId:guid}")]
    [AllowAnonymous]
    public sealed class PropertyReviewsController
        : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public PropertyReviewsController(
            IReviewService reviewService)
        {
            ArgumentNullException.ThrowIfNull(
                reviewService);

            _reviewService = reviewService;
        }

        /*
         * GET:
         * /api/properties/{propertyId}/reviews
         */
        [HttpGet("reviews")]
        [ProducesResponseType(
            typeof(PropertyReviewsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<PropertyReviewsResponse>>
            GetReviewsAsync(
                Guid propertyId,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService
                    .GetPropertyReviewsAsync(
                        propertyId,
                        GetOptionalAuthenticatedUserId(),
                        page,
                        pageSize,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/properties/{propertyId}/rating-summary
         */
        [HttpGet("rating-summary")]
        [ProducesResponseType(
            typeof(PropertyRatingSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<PropertyRatingSummaryResponse>>
            GetRatingSummaryAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _reviewService
                    .GetPropertyRatingSummaryAsync(
                        propertyId,
                        cancellationToken);

            return Ok(response);
        }

        private Guid? GetOptionalAuthenticatedUserId()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

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