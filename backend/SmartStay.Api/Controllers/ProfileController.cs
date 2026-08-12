using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public sealed class ProfileController
        : ControllerBase
    {
        private readonly IProfileService
            _profileService;

        public ProfileController(
            IProfileService profileService)
        {
            ArgumentNullException.ThrowIfNull(
                profileService);

            _profileService =
                profileService;
        }

        /*
         * GET:
         * /api/profile
         */
        [HttpGet]
        [ProducesResponseType(
            typeof(UserProfileResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<UserProfileResponse>>
            GetAsync(
                CancellationToken cancellationToken)
        {
            var response =
                await _profileService.GetAsync(
                    GetAuthenticatedUserId(),
                    cancellationToken);

            return Ok(response);
        }

        /*
         * PUT:
         * /api/profile
         */
        [HttpPut]
        [ProducesResponseType(
            typeof(UserProfileResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<UserProfileResponse>>
            UpdateAsync(
                [FromBody]
                UpdateUserProfileRequest request,
                CancellationToken cancellationToken)
        {
            var response =
                await _profileService.UpdateAsync(
                    GetAuthenticatedUserId(),
                    request,
                    cancellationToken);

            return Ok(response);
        }

        /*
         * POST:
         * /api/profile/image
         *
         * multipart/form-data
         * Field name: file
         */
        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(
            typeof(UserProfileResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<UserProfileResponse>>
            UploadImageAsync(
                IFormFile? file,
                CancellationToken cancellationToken)
        {
            if (file is null ||
                file.Length == 0)
            {
                throw new ArgumentException(
                    "A profile image is required.");
            }

            await using var fileStream =
                file.OpenReadStream();

            var response =
                await _profileService
                    .UploadImageAsync(
                        GetAuthenticatedUserId(),
                        fileStream,
                        file.FileName,
                        file.ContentType,
                        cancellationToken);

            return Ok(response);
        }

        /*
         * DELETE:
         * /api/profile/image
         */
        [HttpDelete("image")]
        [ProducesResponseType(
            typeof(UserProfileResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<UserProfileResponse>>
            DeleteImageAsync(
                CancellationToken cancellationToken)
        {
            var response =
                await _profileService
                    .DeleteImageAsync(
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
                    "The authenticated user identifier is invalid.");
            }

            return userId;
        }
    }
}