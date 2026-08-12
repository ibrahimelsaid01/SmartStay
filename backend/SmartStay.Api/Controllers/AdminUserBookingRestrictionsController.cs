using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminUserBookingRestrictionsController
        : ControllerBase
    {
        private readonly IUserBookingRestrictionService
            _userBookingRestrictionService;

        private readonly IAdminActionLogService
            _adminActionLogService;

        private readonly ILogger<AdminUserBookingRestrictionsController>
            _logger;

        public AdminUserBookingRestrictionsController(
            IUserBookingRestrictionService userBookingRestrictionService,
            IAdminActionLogService adminActionLogService,
            ILogger<AdminUserBookingRestrictionsController> logger)
        {
            ArgumentNullException.ThrowIfNull(
                userBookingRestrictionService);

            ArgumentNullException.ThrowIfNull(
                adminActionLogService);

            ArgumentNullException.ThrowIfNull(
                logger);

            _userBookingRestrictionService =
                userBookingRestrictionService;

            _adminActionLogService =
                adminActionLogService;

            _logger =
                logger;
        }

        [HttpGet("api/admin/users/{userId:guid}/booking-restrictions")]
        [ProducesResponseType(
            typeof(IReadOnlyList<UserBookingRestrictionResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<UserBookingRestrictionResponse>>>
            GetUserRestrictionsAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _userBookingRestrictionService
                    .GetUserRestrictionsAsync(
                        userId,
                        cancellationToken);

            return Ok(
                response);
        }

        [HttpGet("api/admin/users/{userId:guid}/booking-restrictions/active")]
        [ProducesResponseType(
            typeof(UserBookingRestrictionResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserBookingRestrictionResponse>>
            GetActiveRestrictionAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _userBookingRestrictionService
                    .GetActiveBookingRestrictionAsync(
                        userId,
                        cancellationToken);

            if (response is null)
            {
                return NoContent();
            }

            return Ok(
                response);
        }

        [HttpPost("api/admin/user-booking-restrictions/{adminReviewFlagId:guid}/temporary-suspension")]
        [ProducesResponseType(
            typeof(UserBookingRestrictionResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserBookingRestrictionResponse>>
            ApplyTemporaryBookingRestrictionAsync(
                Guid adminReviewFlagId,
                ApplyTemporaryBookingRestrictionRequest request,
                CancellationToken cancellationToken = default)
        {
            var adminUserId =
                GetCurrentUserId();

            var response =
                await _userBookingRestrictionService
                    .ApplyTemporaryBookingRestrictionAsync(
                        adminUserId,
                        adminReviewFlagId,
                        request,
                        cancellationToken);

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.DecisionApplied.ToString(),

                    TargetType =
                        AdminActionTargetType.UserBookingRestriction.ToString(),

                    TargetId =
                        response.RestrictionId,

                    TargetReference =
                        response.UserId.ToString(),

                    Summary =
                        $"Admin applied a temporary booking suspension to user {response.UserId}.",

                    Details =
                        "SourceAdminReviewFlagId: " +
                        $"{adminReviewFlagId}; " +
                        "TemporaryRestrictionId: " +
                        $"{response.RestrictionId}; " +
                        "DurationDays: " +
                        $"{request.DurationDays}; " +
                        "RestrictedUntil: " +
                        $"{response.RestrictedUntil:O}; " +
                        "CancellationCountSnapshot: " +
                        $"{response.CancellationCountSnapshot}; " +
                        "Reason: " +
                        $"{response.Reason}",

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            return Ok(
                response);
        }

        [HttpPatch("api/admin/user-booking-restrictions/{restrictionId:guid}/remove")]
        [ProducesResponseType(
            typeof(UserBookingRestrictionResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserBookingRestrictionResponse>>
            RemoveRestrictionAsync(
                Guid restrictionId,
                RemoveUserBookingRestrictionRequest request,
                CancellationToken cancellationToken = default)
        {
            var adminUserId =
                GetCurrentUserId();

            var response =
                await _userBookingRestrictionService
                    .RemoveRestrictionAsync(
                        adminUserId,
                        restrictionId,
                        request,
                        cancellationToken);

            await TryCreateAdminActionLogAsync(
                adminUserId,
                new CreateAdminActionLogRequest
                {
                    ActionType =
                        AdminActionType.UserRestrictionRemoved.ToString(),

                    TargetType =
                        AdminActionTargetType.UserBookingRestriction.ToString(),

                    TargetId =
                        response.RestrictionId,

                    TargetReference =
                        response.UserId.ToString(),

                    Summary =
                        $"Admin removed booking restriction {response.RestrictionId} for user {response.UserId}.",

                    Details =
                        "RestrictionType: " +
                        $"{response.Type}; " +
                        "CancellationCountSnapshot: " +
                        $"{response.CancellationCountSnapshot}; " +
                        "RemovalNote: " +
                        $"{request.RemovalNote ?? "N/A"}",

                    IpAddress =
                        GetClientIpAddress(),

                    UserAgent =
                        GetUserAgent()
                },
                cancellationToken);

            return Ok(
                response);
        }

        private async Task TryCreateAdminActionLogAsync(
            Guid adminUserId,
            CreateAdminActionLogRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                await _adminActionLogService
                    .CreateAsync(
                        adminUserId,
                        request,
                        cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to create admin action log. ActionType: {ActionType}, TargetType: {TargetType}, TargetId: {TargetId}.",
                    request.ActionType,
                    request.TargetType,
                    request.TargetId);
            }
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

        private string? GetClientIpAddress()
        {
            return HttpContext
                .Connection
                .RemoteIpAddress?
                .ToString();
        }

        private string? GetUserAgent()
        {
            var userAgent =
                Request.Headers["User-Agent"]
                    .ToString();

            return string.IsNullOrWhiteSpace(
                    userAgent)
                ? null
                : userAgent;
        }
    }
}