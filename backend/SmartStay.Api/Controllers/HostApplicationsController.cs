using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using System.Security.Claims;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/host-applications")]
    [Authorize]
    public sealed class HostApplicationsController
        : ControllerBase
    {
        private readonly IHostApplicationService
            _hostApplicationService;

        public HostApplicationsController(
            IHostApplicationService hostApplicationService)
        {
            _hostApplicationService =
                hostApplicationService;
        }

        [HttpPost("draft")]
        [ProducesResponseType(
            typeof(HostApplicationResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<HostApplicationResponse>>
            CreateDraftAsync(
                [FromBody]
                CreateHostApplicationRequest request,
                CancellationToken cancellationToken)
        {
            var userId =
                GetAuthenticatedUserId();

            var response =
                await _hostApplicationService
                    .CreateDraftAsync(
                        userId,
                        request,
                        cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }



        [HttpGet("current")]
        public async Task<ActionResult<HostApplicationResponse>>
        GetCurrentAsync(
          CancellationToken cancellationToken)
        {
            var userId =
                GetAuthenticatedUserId();

            var response =
                await _hostApplicationService
                    .GetCurrentAsync(
                        userId,
                        cancellationToken);

            return Ok(response);
        }




        [HttpPut("current")]
        public async Task<ActionResult<HostApplicationResponse>>
    UpdateCurrentAsync(
        [FromBody]
        UpdateHostApplicationRequest request,
        CancellationToken cancellationToken)
        {
            var userId =
                GetAuthenticatedUserId();

            var response =
                await _hostApplicationService
                    .UpdateCurrentAsync(
                        userId,
                        request,
                        cancellationToken);

            return Ok(response);
        }




        [HttpPost("current/profile-image")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<HostApplicationResponse>>
    UploadProfileImageAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
        {
            if (file is null ||
                file.Length == 0)
            {
                throw new ArgumentException(
                    "A host profile image is required.");
            }

            using var fileStream =
                file.OpenReadStream();

            var response =
                await _hostApplicationService
                    .UploadProfileImageAsync(
                        GetAuthenticatedUserId(),
                        fileStream,
                        file.FileName,
                        file.ContentType,
                        cancellationToken);

            return Ok(response);
        }





        [HttpPost("current/national-id")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<HostApplicationResponse>>
    UploadNationalIdAsync(
        [FromForm]
        UploadHostNationalIdForm form,
        CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(form);

            if (form.FrontFile is null ||
                form.FrontFile.Length == 0)
            {
                throw new ArgumentException(
                    "The front image of the national ID is required.");
            }

            if (form.BackFile is null ||
                form.BackFile.Length == 0)
            {
                throw new ArgumentException(
                    "The back image of the national ID is required.");
            }

            using var frontFileStream =
                form.FrontFile.OpenReadStream();

            using var backFileStream =
                form.BackFile.OpenReadStream();

            var response =
                await _hostApplicationService
                    .UploadNationalIdAsync(
                        GetAuthenticatedUserId(),

                        frontFileStream,
                        form.FrontFile.FileName,
                        form.FrontFile.ContentType,

                        backFileStream,
                        form.BackFile.FileName,
                        form.BackFile.ContentType,

                        cancellationToken);

            return Ok(response);
        }





        [HttpPost("current/submit")]
        public async Task<ActionResult<HostApplicationResponse>>
        SubmitCurrentAsync(
            CancellationToken cancellationToken)
            {
            var userId =
                GetAuthenticatedUserId();

            var response =
                await _hostApplicationService
                    .SubmitCurrentAsync(
                        userId,
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