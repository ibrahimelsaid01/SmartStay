using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using System.Security.Claims;

namespace SmartStay.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController
        : ControllerBase
    {
        private const string RefreshTokenCookieName =
            "SmartStay.RefreshToken";

        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService)
        {
            ArgumentNullException.ThrowIfNull(
                authService);

            _authService =
                authService;
        }

        [AllowAnonymous]
        [HttpPost("otp/send")]
        public async Task<ActionResult<SendOtpResult>>
            SendOtp(
                [FromBody] SendOtpRequest request,
                CancellationToken cancellationToken)
        {
            var result =
                await _authService.SendOtpAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("otp/verify")]
        public async Task<ActionResult<AuthResponse>>
            VerifyOtp(
                [FromBody] VerifyOtpRequest request,
                CancellationToken cancellationToken)
        {
            var result =
                await _authService.VerifyOtpAsync(
                    request,
                    GetClientIpAddress(),
                    cancellationToken);

            SetRefreshTokenCookie(
                result.RefreshToken,
                result.RefreshTokenExpiresAt);

            return Ok(
                MapAuthResponse(
                    result));
        }

        [AllowAnonymous]
        [HttpPost("external-login")]
        public async Task<ActionResult<AuthResponse>>
            ExternalLogin(
                [FromBody] ExternalLoginRequest request,
                CancellationToken cancellationToken)
        {
            var result =
                await _authService.ExternalLoginAsync(
                    request,
                    GetClientIpAddress(),
                    cancellationToken);

            SetRefreshTokenCookie(
                result.RefreshToken,
                result.RefreshTokenExpiresAt);

            return Ok(
                MapAuthResponse(
                    result));
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>>
            Refresh(
                CancellationToken cancellationToken)
        {
            var refreshToken =
                Request.Cookies[
                    RefreshTokenCookieName];

            if (string.IsNullOrWhiteSpace(
                    refreshToken))
            {
                throw new UnauthorizedAccessException(
                    "Refresh token is missing.");
            }

            var result =
                await _authService.RefreshAsync(
                    refreshToken,
                    GetClientIpAddress(),
                    cancellationToken);

            SetRefreshTokenCookie(
                result.RefreshToken,
                result.RefreshTokenExpiresAt);

            return Ok(
                MapAuthResponse(
                    result));
        }

        [Authorize]
        [HttpPatch("complete-profile")]
        public async Task<
            ActionResult<AuthenticatedUserResponse>>
            CompleteProfile(
                [FromBody] CompleteProfileRequest request,
                CancellationToken cancellationToken)
        {
            var userId =
                GetCurrentUserId();

            var user =
                await _authService.CompleteProfileAsync(
                    userId,
                    request,
                    cancellationToken);

            return Ok(user);
        }

        [Authorize]
        [HttpGet("current-user")]
        public async Task<
            ActionResult<AuthenticatedUserResponse>>
            GetCurrentUser(
                CancellationToken cancellationToken)
        {
            var userId =
                GetCurrentUserId();

            var user =
                await _authService.GetCurrentUserAsync(
                    userId,
                    cancellationToken);

            return Ok(user);
        }

        /*
         * Logs out only the device represented by the
         * refresh-token cookie attached to this request.
         *
         * This endpoint remains anonymous so an expired
         * access token does not prevent local logout.
         */
        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(
            CancellationToken cancellationToken)
        {
            var refreshToken =
                Request.Cookies[
                    RefreshTokenCookieName];

            if (!string.IsNullOrWhiteSpace(
                    refreshToken))
            {
                await _authService.LogoutAsync(
                    refreshToken,
                    GetClientIpAddress(),
                    cancellationToken);
            }

            DeleteRefreshTokenCookie();

            return NoContent();
        }

        /*
         * Revokes every active refresh token belonging
         * to the authenticated user.
         *
         * POST /api/auth/logout-all-devices
         */
        [Authorize]
        [HttpPost("logout-all-devices")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult>
            LogoutFromAllDevices(
                CancellationToken cancellationToken)
        {
            await _authService
                .LogoutFromAllDevicesAsync(
                    GetCurrentUserId(),
                    GetClientIpAddress(),
                    cancellationToken);

            /*
             * Delete the cookie from the device that initiated
             * the operation. Cookies on other devices cannot be
             * deleted remotely, but their server-side refresh
             * tokens have now been revoked.
             */
            DeleteRefreshTokenCookie();

            return NoContent();
        }

        private void SetRefreshTokenCookie(
            string refreshToken,
            DateTimeOffset expiresAt)
        {
            Response.Cookies.Append(
                RefreshTokenCookieName,
                refreshToken,
                new CookieOptions
                {
                    HttpOnly =
                        true,

                    Secure =
                        true,

                    SameSite =
                        SameSiteMode.None,

                    IsEssential =
                        true,

                    Expires =
                        expiresAt,

                    Path =
                        "/api/auth"
                });
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete(
                RefreshTokenCookieName,
                new CookieOptions
                {
                    HttpOnly =
                        true,

                    Secure =
                        true,

                    SameSite =
                        SameSiteMode.None,

                    Path =
                        "/api/auth"
                });
        }

        private Guid GetCurrentUserId()
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(
                    userIdValue,
                    out var userId))
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user identifier is invalid.");
            }

            return userId;
        }

        private string? GetClientIpAddress()
        {
            return HttpContext.Connection
                .RemoteIpAddress?
                .ToString();
        }

        private static AuthResponse MapAuthResponse(
            AuthResult result)
        {
            return new AuthResponse
            {
                AccessToken =
                    result.AccessToken,

                AccessTokenExpiresAt =
                    result.AccessTokenExpiresAt,

                IsNewUser =
                    result.IsNewUser,

                NextStep =
                    result.NextStep,

                User =
                    result.User
            };
        }
    }
}