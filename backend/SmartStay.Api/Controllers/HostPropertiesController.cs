using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;
using System.Security.Claims;

[ApiController]
[Route("api/host/properties")]
[Authorize(Roles = RoleNames.Host)]
public sealed class HostPropertiesController
    : ControllerBase
{
    private readonly IHostPropertyService
        _hostPropertyService;

    public HostPropertiesController(
        IHostPropertyService hostPropertyService)
    {
        ArgumentNullException.ThrowIfNull(
            hostPropertyService);

        _hostPropertyService =
            hostPropertyService;
    }

    [HttpPost("draft")]
    public async Task<
        ActionResult<PropertyDraftResponse>>
        CreateDraftAsync(
            [FromBody]
            CreatePropertyDraftRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .CreateDraftAsync(
                    GetAuthenticatedUserId(),
                    request,
                    cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpGet("{propertyId:guid}")]
    public async Task<
        ActionResult<PropertyDraftResponse>>
        GetByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .GetByIdAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpGet("{propertyId:guid}/editor")]
    public async Task<
        ActionResult<PropertyEditorResponse>>
        GetEditorAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .GetEditorAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut(
        "{propertyId:guid}/basic-information")]
    public async Task<
        ActionResult<PropertyDraftResponse>>
        UpdateBasicInformationAsync(
            Guid propertyId,
            [FromBody]
            UpdatePropertyBasicInformationRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .UpdateBasicInformationAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut("{propertyId:guid}/location")]
    public async Task<
        ActionResult<PropertyLocationResponse>>
        UpdateLocationAsync(
            Guid propertyId,
            [FromBody]
            UpdatePropertyLocationRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .UpdateLocationAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut("{propertyId:guid}/capacity")]
    public async Task<
        ActionResult<PropertyCapacityResponse>>
        UpdateCapacityAsync(
            Guid propertyId,
            [FromBody]
            UpdatePropertyCapacityRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .UpdateCapacityAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut(
        "{propertyId:guid}/pricing-and-policies")]
    public async Task<
        ActionResult<
            PropertyPricingAndPoliciesResponse>>
        UpdatePricingAndPoliciesAsync(
            Guid propertyId,
            [FromBody]
            UpdatePropertyPricingAndPoliciesRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .UpdatePricingAndPoliciesAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut("{propertyId:guid}/house-rules")]
    public async Task<
        ActionResult<PropertyHouseRulesResponse>>
        UpdateHouseRulesAsync(
            Guid propertyId,
            [FromBody]
            UpdatePropertyHouseRulesRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .UpdateHouseRulesAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpGet("{propertyId:guid}/amenities")]
    public async Task<
        ActionResult<PropertyAmenitiesResponse>>
        GetAmenitiesAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .GetAmenitiesAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut("{propertyId:guid}/amenities")]
    public async Task<
        ActionResult<PropertyAmenitiesResponse>>
        UpdateAmenitiesAsync(
            Guid propertyId,
            [FromBody]
            UpdatePropertyAmenitiesRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .UpdateAmenitiesAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpGet("{propertyId:guid}/images")]
    public async Task<
        ActionResult<PropertyImagesResponse>>
        GetImagesAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .GetImagesAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPost("{propertyId:guid}/images")]
    [Consumes("multipart/form-data")]
    public async Task<
        ActionResult<PropertyImagesResponse>>
        UploadImagesAsync(
            Guid propertyId,
            [FromForm]
            UploadPropertyImagesRequest request,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.Files is null ||
            request.Files.Count == 0)
        {
            throw new ArgumentException(
                "At least one image is required.");
        }

        var response =
            await _hostPropertyService
                .UploadImagesAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request.Files,
                    cancellationToken);

        return Ok(response);
    }

    [HttpDelete(
        "{propertyId:guid}/images/{imageId:guid}")]
    public async Task<
        ActionResult<PropertyImagesResponse>>
        DeleteImageAsync(
            Guid propertyId,
            Guid imageId,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .DeleteImageAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    imageId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut(
        "{propertyId:guid}/images/" +
        "{imageId:guid}/cover")]
    public async Task<
        ActionResult<PropertyImagesResponse>>
        SetCoverImageAsync(
            Guid propertyId,
            Guid imageId,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .SetCoverImageAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    imageId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPut("{propertyId:guid}/images/order")]
    public async Task<
        ActionResult<PropertyImagesResponse>>
        UpdateImageOrderAsync(
            Guid propertyId,
            [FromBody]
            UpdatePropertyImageOrderRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .UpdateImageOrderAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request,
                    cancellationToken);

        return Ok(response);
    }

    [HttpGet(
        "{propertyId:guid}/verification-document")]
    public async Task<
        ActionResult<
            PropertyVerificationDocumentResponse>>
        GetVerificationDocumentAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .GetVerificationDocumentAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    cancellationToken);

        return Ok(response);
    }

    [HttpPost(
        "{propertyId:guid}/verification-document")]
    [Consumes("multipart/form-data")]
    public async Task<
        ActionResult<
            PropertyVerificationDocumentResponse>>
        UploadVerificationDocumentAsync(
            Guid propertyId,
            [FromForm]
            UploadPropertyVerificationDocumentRequest
                request,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (!request.DocumentType.HasValue)
        {
            throw new ArgumentException(
                "The verification document type is required.");
        }

        if (request.Files is null ||
            request.Files.Count == 0)
        {
            throw new ArgumentException(
                "At least one document page is required.");
        }

        var response =
            await _hostPropertyService
                .UploadVerificationDocumentAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    request.DocumentType.Value,
                    request.Files,
                    cancellationToken);

        return Ok(response);
    }

    [HttpDelete(
        "{propertyId:guid}/verification-document")]
    public async Task<IActionResult>
        DeleteVerificationDocumentAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
    {
        await _hostPropertyService
            .DeleteVerificationDocumentAsync(
                GetAuthenticatedUserId(),
                propertyId,
                cancellationToken);

        return NoContent();
    }

    [HttpGet(
        "{propertyId:guid}/verification-document/" +
        "pages/{pageId:guid}/content")]
    [Produces(
        "image/jpeg",
        "image/png",
        "image/webp")]
    public async Task<IActionResult>
        GetVerificationDocumentPageContentAsync(
            Guid propertyId,
            Guid pageId,
            CancellationToken cancellationToken)
    {
        var imageContent =
            await _hostPropertyService
                .GetVerificationDocumentPageContentAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    pageId,
                    cancellationToken);

        return CreateSensitiveImageResult(
            imageContent);
    }

    [HttpPost("{propertyId:guid}/submit")]
    public async Task<
        ActionResult<PropertySubmissionResponse>>
        SubmitAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
    {
        var response =
            await _hostPropertyService
                .SubmitAsync(
                    GetAuthenticatedUserId(),
                    propertyId,
                    cancellationToken);

        return StatusCode(
            StatusCodes.Status202Accepted,
            response);
    }

    private FileContentResult
        CreateSensitiveImageResult(
            ImageContentResult image)
    {
        Response.Headers.CacheControl =
            "no-store, no-cache, max-age=0";

        Response.Headers.Pragma =
            "no-cache";

        Response.Headers[
            "X-Content-Type-Options"] =
                "nosniff";

        return File(
            image.Content,
            image.ContentType);
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