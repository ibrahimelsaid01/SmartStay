using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminUsersController
        : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(
            IAdminUserService adminUserService)
        {
            ArgumentNullException.ThrowIfNull(
                adminUserService);

            _adminUserService =
                adminUserService;
        }

        /*
         * GET:
         * /api/admin/users
         *
         * Examples:
         *
         * /api/admin/users?page=1&pageSize=20
         *
         * /api/admin/users?search=ahmed
         *
         * /api/admin/users?role=Host&isActive=true
         */
        [HttpGet]
        [ProducesResponseType(
            typeof(AdminUsersResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminUsersResponse>>
            GetUsersAsync(
                [FromQuery] AdminUserSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminUserService
                    .GetUsersAsync(
                        request,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/admin/users/{userId}
         */
        [HttpGet("{userId:guid}")]
        [ProducesResponseType(
            typeof(AdminUserDetailsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminUserDetailsResponse>>
            GetUserByIdAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminUserService
                    .GetUserByIdAsync(
                        userId,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * PATCH:
         * /api/admin/users/{userId}/deactivate
         *
         * This is the backend equivalent of
         * "Suspend Account" in the Admin UI.
         */
        [HttpPatch("{userId:guid}/deactivate")]
        [ProducesResponseType(
            typeof(AdminUserStatusResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminUserStatusResponse>>
            DeactivateUserAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var currentAdminUserId =
                GetCurrentUserId();

            var response =
                await _adminUserService
                    .DeactivateUserAsync(
                        currentAdminUserId,
                        userId,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * PATCH:
         * /api/admin/users/{userId}/activate
         */
        [HttpPatch("{userId:guid}/activate")]
        [ProducesResponseType(
            typeof(AdminUserStatusResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdminUserStatusResponse>>
            ActivateUserAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminUserService
                    .ActivateUserAsync(
                        userId,
                        cancellationToken);

            return Ok(response);
        }

        private Guid GetCurrentUserId()
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ??
                User.FindFirstValue(
                    "sub");

            if (!Guid.TryParse(
                    userIdValue,
                    out var userId)
                ||
                userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain a valid user identifier.");
            }

            return userId;
        }
    }
}