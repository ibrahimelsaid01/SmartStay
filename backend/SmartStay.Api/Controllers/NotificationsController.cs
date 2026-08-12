using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using System.Security.Claims;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public sealed class NotificationsController
        : ControllerBase
    {
        private readonly INotificationService
            _notificationService;

        public NotificationsController(
            INotificationService notificationService)
        {
            ArgumentNullException.ThrowIfNull(
                notificationService);

            _notificationService =
                notificationService;
        }

        /*
         * GET:
         * /api/notifications
         *
         * Examples:
         *
         * /api/notifications?page=1&pageSize=20
         *
         * /api/notifications
         * ?unreadOnly=true&page=1&pageSize=20
         */
        [HttpGet]
        [ProducesResponseType(
            typeof(NotificationsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<
            ActionResult<NotificationsResponse>>
            GetNotificationsAsync(
                [FromQuery] bool unreadOnly = false,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _notificationService
                    .GetNotificationsAsync(
                        GetAuthenticatedUserId(),
                        unreadOnly,
                        page,
                        pageSize,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * GET:
         * /api/notifications/unread-count
         */
        [HttpGet("unread-count")]
        [ProducesResponseType(
            typeof(UnreadNotificationsCountResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<
            ActionResult<UnreadNotificationsCountResponse>>
            GetUnreadCountAsync(
                CancellationToken cancellationToken = default)
        {
            var response =
                await _notificationService
                    .GetUnreadCountAsync(
                        GetAuthenticatedUserId(),
                        cancellationToken);

            return Ok(response);
        }

        /*
         * PATCH:
         * /api/notifications/{notificationId}/read
         */
        [HttpPatch("{notificationId:guid}/read")]
        [ProducesResponseType(
            typeof(NotificationResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<NotificationResponse>>
            MarkAsReadAsync(
                Guid notificationId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _notificationService
                    .MarkAsReadAsync(
                        GetAuthenticatedUserId(),
                        notificationId,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * PATCH:
         * /api/notifications/read-all
         */
        [HttpPatch("read-all")]
        [ProducesResponseType(
            typeof(MarkAllNotificationsReadResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<
            ActionResult<MarkAllNotificationsReadResponse>>
            MarkAllAsReadAsync(
                CancellationToken cancellationToken = default)
        {
            var response =
                await _notificationService
                    .MarkAllAsReadAsync(
                        GetAuthenticatedUserId(),
                        cancellationToken);

            return Ok(response);
        }

        /*
         * DELETE:
         * /api/notifications/{notificationId}
         */
        [HttpDelete("{notificationId:guid}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            await _notificationService
                .DeleteAsync(
                    GetAuthenticatedUserId(),
                    notificationId,
                    cancellationToken);

            return NoContent();
        }

        /*
         * DELETE:
         * /api/notifications
         *
         * Deletes only the authenticated user's
         * notifications.
         */
        [HttpDelete]
        [ProducesResponseType(
            typeof(DeleteAllNotificationsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<
            ActionResult<DeleteAllNotificationsResponse>>
            DeleteAllAsync(
                CancellationToken cancellationToken = default)
        {
            var response =
                await _notificationService
                    .DeleteAllAsync(
                        GetAuthenticatedUserId(),
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