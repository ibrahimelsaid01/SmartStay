using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using System.Security.Claims;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/account")]
    [Authorize]
    public sealed class AccountController
        : ControllerBase
    {
        private const string RefreshTokenCookieName =
            "SmartStay.RefreshToken";

        private readonly IAccountService
            _accountService;

        public AccountController(
            IAccountService accountService)
        {
            ArgumentNullException.ThrowIfNull(
                accountService);

            _accountService =
                accountService;
        }

        /*
         * POST:
         * /api/account/deactivate
         */
        [HttpPost("deactivate")]
        [ProducesResponseType(
            typeof(AccountDeactivationResponse),
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
            ActionResult<AccountDeactivationResponse>>
            DeactivateAsync(
                [FromBody]
                DeactivateAccountRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _accountService
                    .DeactivateAsync(
                        GetAuthenticatedUserId(),
                        request,
                        GetClientIpAddress(),
                        cancellationToken);

            /*
             * The refresh token was revoked in the database.
             * Delete its cookie from the current browser too.
             */
            DeleteRefreshTokenCookie();

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

        private string? GetClientIpAddress()
        {
            return HttpContext.Connection
                .RemoteIpAddress?
                .ToString();
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

                    /*
                     * Must match the path used when
                     * the cookie was originally created.
                     */
                    Path =
                        "/api/auth"
                });
        }
    }
}