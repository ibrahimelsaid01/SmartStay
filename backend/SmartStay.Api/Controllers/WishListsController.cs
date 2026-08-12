using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/wishlists")]
    [Authorize]
    public sealed class WishListsController
        : ControllerBase
    {
        private readonly IWishListService
            _wishListService;

        public WishListsController(
            IWishListService wishListService)
        {
            ArgumentNullException.ThrowIfNull(
                wishListService);

            _wishListService =
                wishListService;
        }

        /*
         * GET:
         * /api/wishlists
         *
         * لمعرفة هل عقار معين موجود في القوائم:
         *
         * /api/wishlists?propertyId={propertyId}
         */
        [HttpGet]
        [ProducesResponseType(
            typeof(WishListsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WishListsResponse>>
            GetAllAsync(
                [FromQuery] Guid? propertyId = null,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService.GetAllAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/wishlists/{wishListId}
         */
        [HttpGet("{wishListId:guid}")]
        [ProducesResponseType(
            typeof(WishListDetailsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<WishListDetailsResponse>>
            GetByIdAsync(
                Guid wishListId,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 12,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService.GetByIdAsync(
                    GetAuthenticatedUserId(),
                    wishListId,
                    page,
                    pageSize,
                    cancellationToken);

            return Ok(response);
        }

        /*
         * POST:
         * /api/wishlists
         */
        [HttpPost]
        [ProducesResponseType(
            typeof(WishListSummaryResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<
            ActionResult<WishListSummaryResponse>>
            CreateAsync(
                [FromBody]
                CreateWishListRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService.CreateAsync(
                    GetAuthenticatedUserId(),
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetByIdAsync),
                new
                {
                    wishListId = response.Id
                },
                response);
        }

        /*
         * PUT:
         * /api/wishlists/{wishListId}
         */
        [HttpPut("{wishListId:guid}")]
        [ProducesResponseType(
            typeof(WishListSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<
            ActionResult<WishListSummaryResponse>>
            UpdateAsync(
                Guid wishListId,
                [FromBody]
                UpdateWishListRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService.UpdateAsync(
                    GetAuthenticatedUserId(),
                    wishListId,
                    request,
                    cancellationToken);

            return Ok(response);
        }

        /*
         * DELETE:
         * /api/wishlists/{wishListId}
         */
        [HttpDelete("{wishListId:guid}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(
            Guid wishListId,
            CancellationToken cancellationToken = default)
        {
            await _wishListService.DeleteAsync(
                GetAuthenticatedUserId(),
                wishListId,
                cancellationToken);

            return NoContent();
        }

        /*
         * POST:
         * /api/wishlists/{wishListId}/items
         */
        [HttpPost("{wishListId:guid}/items")]
        [ProducesResponseType(
            typeof(WishListItemResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<
            ActionResult<WishListItemResponse>>
            AddItemAsync(
                Guid wishListId,
                [FromBody]
                AddWishListItemRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService.AddItemAsync(
                    GetAuthenticatedUserId(),
                    wishListId,
                    request,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }

        /*
         * DELETE:
         * /api/wishlists/{wishListId}/items/{propertyId}
         */
        [HttpDelete(
            "{wishListId:guid}/items/{propertyId:guid}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveItemAsync(
            Guid wishListId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            await _wishListService.RemoveItemAsync(
                GetAuthenticatedUserId(),
                wishListId,
                propertyId,
                cancellationToken);

            return NoContent();
        }

        /*
         * PUT:
         * /api/wishlists/{wishListId}/items/{propertyId}/note
         */
        [HttpPut(
            "{wishListId:guid}/items/" +
            "{propertyId:guid}/note")]
        [ProducesResponseType(
            typeof(WishListItemResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<WishListItemResponse>>
            UpdateItemNoteAsync(
                Guid wishListId,
                Guid propertyId,
                [FromBody]
                UpdateWishListItemNoteRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService.UpdateItemNoteAsync(
                    GetAuthenticatedUserId(),
                    wishListId,
                    propertyId,
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