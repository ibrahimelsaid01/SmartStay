using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/support/tickets")]
    [Authorize]
    public sealed class SupportTicketsController : ControllerBase
    {
        private const long MaximumEvidenceFileSizeInBytes =
            5 * 1024 * 1024;

        private static readonly IReadOnlyDictionary<string, string>
            AllowedEvidenceContentTypes =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    [".jpg"] = "image/jpeg",
                    [".jpeg"] = "image/jpeg",
                    [".png"] = "image/png",
                    [".webp"] = "image/webp"
                };

        private readonly ISupportTicketService
            _supportTicketService;

        private readonly SmartStayDbContext
            _dbContext;

        public SupportTicketsController(
            ISupportTicketService supportTicketService,
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                supportTicketService);

            ArgumentNullException.ThrowIfNull(
                dbContext);

            _supportTicketService =
                supportTicketService;

            _dbContext =
                dbContext;
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(SupportTicketResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupportTicketResponse>>
            CreateTicketAsync(
                CreateSupportTicketRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var userId =
                GetCurrentUserId();

            await NormalizeAndValidateTicketRequestAsync(
                userId,
                request,
                cancellationToken);

            var response =
                await _supportTicketService.CreateTicketAsync(
                    userId,
                    request,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }

        [HttpGet("my-tickets")]
        [ProducesResponseType(
            typeof(SupportTicketsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SupportTicketsResponse>>
            GetMyTicketsAsync(
                [FromQuery] SupportTicketSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var userId =
                GetCurrentUserId();

            var response =
                await _supportTicketService.GetMyTicketsAsync(
                    userId,
                    request,
                    cancellationToken);

            return Ok(
                response);
        }

        [HttpGet("{ticketId:guid}")]
        [ProducesResponseType(
            typeof(SupportTicketResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupportTicketResponse>>
            GetTicketByIdAsync(
                Guid ticketId,
                CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(
                ticketId);

            var userId =
                GetCurrentUserId();

            var response =
                await _supportTicketService.GetMyTicketByIdAsync(
                    userId,
                    ticketId,
                    cancellationToken);

            return Ok(
                response);
        }

        [HttpPost("{ticketId:guid}/messages")]
        [ProducesResponseType(
            typeof(SupportTicketResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupportTicketResponse>>
            AddMessageAsync(
                Guid ticketId,
                CreateSupportTicketMessageRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(
                ticketId);

            ArgumentNullException.ThrowIfNull(
                request);

            var userId =
                GetCurrentUserId();

            var response =
                await _supportTicketService.AddUserMessageAsync(
                    userId,
                    ticketId,
                    request,
                    cancellationToken);

            return Ok(
                response);
        }

        [HttpPost("{ticketId:guid}/attachments")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(
            typeof(UploadSupportTicketAttachmentResponse),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UploadSupportTicketAttachmentResponse>>
            UploadAttachmentAsync(
                Guid ticketId,
                IFormFile file,
                [FromForm] string? type = null,
                CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(
                ticketId);

            var userId =
                GetCurrentUserId();

            ValidateEvidenceImage(
                file);

            var attachmentType =
                ParseCanonicalAttachmentType(
                    type);

            var response =
                await _supportTicketService.UploadUserAttachmentAsync(
                    userId,
                    ticketId,
                    file,
                    attachmentType.ToString(),
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }

        private async Task NormalizeAndValidateTicketRequestAsync(
            Guid userId,
            CreateSupportTicketRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var category =
                ParseCanonicalCategory(
                    request.Category);

            var urgency =
                ParseCanonicalUrgency(
                    request.Urgency);

            request.Category =
                category.ToString();

            request.Urgency =
                urgency.ToString();

            ValidateOptionalIdentifier(
                request.BookingId,
                "The booking identifier is invalid.");

            ValidateOptionalIdentifier(
                request.PropertyId,
                "The property identifier is invalid.");

            if (RequiresBookingReference(category)
                &&
                !request.BookingId.HasValue)
            {
                throw new ArgumentException(
                    "Payment, booking, and refund issues must be linked to one of your bookings.");
            }

            if (!request.BookingId.HasValue)
            {
                return;
            }

            var bookingReference =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .Where(booking =>
                        booking.Id ==
                            request.BookingId.Value
                        &&
                        booking.GuestUserId ==
                            userId)
                    .Select(booking =>
                        new
                        {
                            booking.PropertyId
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (bookingReference is null)
            {
                throw new KeyNotFoundException(
                    "The booking was not found in your account.");
            }

            if (request.PropertyId.HasValue
                &&
                request.PropertyId.Value !=
                    bookingReference.PropertyId)
            {
                throw new ArgumentException(
                    "The selected property does not belong to the referenced booking.");
            }

            /*
             * The booking is the authoritative reference.
             *
             * PropertyId is derived server-side so the user
             * cannot link the complaint to an unrelated
             * property by manipulating the request payload.
             */
            request.PropertyId =
                bookingReference.PropertyId;
        }

        private static SupportTicketCategory
            ParseCanonicalCategory(
                string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value)
                ||
                !Enum.TryParse<SupportTicketCategory>(
                    value.Trim(),
                    ignoreCase: true,
                    out var category)
                ||
                !Enum.IsDefined(
                    category))
            {
                throw new ArgumentException(
                    "The support ticket category is invalid.");
            }

            return category;
        }

        private static SupportTicketUrgency
            ParseCanonicalUrgency(
                string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value)
                ||
                !Enum.TryParse<SupportTicketUrgency>(
                    value.Trim(),
                    ignoreCase: true,
                    out var urgency)
                ||
                !Enum.IsDefined(
                    urgency))
            {
                throw new ArgumentException(
                    "The support ticket urgency is invalid.");
            }

            return urgency;
        }

        private static SupportTicketAttachmentType
            ParseCanonicalAttachmentType(
                string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return SupportTicketAttachmentType
                    .IssueEvidence;
            }

            if (!Enum.TryParse<SupportTicketAttachmentType>(
                    value.Trim(),
                    ignoreCase: true,
                    out var attachmentType)
                ||
                !Enum.IsDefined(
                    attachmentType))
            {
                throw new ArgumentException(
                    "The evidence type must be PropertyPhoto, SelfieAtProperty, IssueEvidence, PaymentEvidence, or Other.");
            }

            return attachmentType;
        }

        private static bool RequiresBookingReference(
            SupportTicketCategory category)
        {
            return category is
                SupportTicketCategory.PaymentIssue
                or SupportTicketCategory.BookingIssue
                or SupportTicketCategory.RefundIssue;
        }

        private static void ValidateEvidenceImage(
            IFormFile file)
        {
            ArgumentNullException.ThrowIfNull(
                file);

            if (file.Length <= 0)
            {
                throw new ArgumentException(
                    "The uploaded evidence image is empty.");
            }

            if (file.Length >
                MaximumEvidenceFileSizeInBytes)
            {
                throw new ArgumentException(
                    "The evidence image size must not exceed 5 MB.");
            }

            if (string.IsNullOrWhiteSpace(
                    file.FileName))
            {
                throw new ArgumentException(
                    "The evidence image file name is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    file.ContentType))
            {
                throw new ArgumentException(
                    "The evidence image content type is required.");
            }

            var extension =
                Path.GetExtension(
                    file.FileName)
                    .ToLowerInvariant();

            if (!AllowedEvidenceContentTypes.TryGetValue(
                    extension,
                    out var expectedContentType))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG, and WebP evidence images are allowed.");
            }

            if (!string.Equals(
                    file.ContentType,
                    expectedContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "The evidence image extension does not match its content type.");
            }
        }

        private static void ValidateTicketIdentifier(
            Guid ticketId)
        {
            if (ticketId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The support ticket identifier is invalid.");
            }
        }

        private static void ValidateOptionalIdentifier(
            Guid? identifier,
            string errorMessage)
        {
            if (identifier.HasValue
                &&
                identifier.Value == Guid.Empty)
            {
                throw new ArgumentException(
                    errorMessage);
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
    }
}