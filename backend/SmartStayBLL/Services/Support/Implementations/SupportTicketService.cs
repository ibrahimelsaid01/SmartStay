using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class SupportTicketService : ISupportTicketService
    {
        private const int MaximumPageSize = 100;
        private const long MaximumAttachmentSizeInBytes = 5 * 1024 * 1024;

        private static readonly IReadOnlyDictionary<string, string>
            AllowedImageContentTypes =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [".jpg"] = "image/jpeg",
                    [".jpeg"] = "image/jpeg",
                    [".png"] = "image/png",
                    [".webp"] = "image/webp"
                };

        private readonly SmartStayDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageStorageService _imageStorageService;
        private readonly CloudinarySettings _cloudinarySettings;
        private readonly IBookingPayoutService _bookingPayoutService;

        public SupportTicketService(
            SmartStayDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IImageStorageService imageStorageService,
            IOptions<CloudinarySettings> cloudinaryOptions,
            IBookingPayoutService bookingPayoutService)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(userManager);
            ArgumentNullException.ThrowIfNull(imageStorageService);
            ArgumentNullException.ThrowIfNull(cloudinaryOptions);
            ArgumentNullException.ThrowIfNull(bookingPayoutService);

            _dbContext = dbContext;
            _userManager = userManager;
            _imageStorageService = imageStorageService;
            _cloudinarySettings = cloudinaryOptions.Value;
            _bookingPayoutService = bookingPayoutService;
        }

        // =====================================================
        // User operations
        // =====================================================

        public async Task<SupportTicketResponse> CreateTicketAsync(
            Guid userId,
            CreateSupportTicketRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(userId);
            ArgumentNullException.ThrowIfNull(request);

            var subject = NormalizeRequiredText(
                request.Subject,
                "The ticket subject is required.",
                200);

            var description = NormalizeRequiredText(
                request.Description,
                "The ticket description is required.",
                4000);

            var category = ParseCategory(request.Category);
            var urgency = ParseUrgency(request.Urgency);

            await EnsureActiveUserExistsAsync(userId, cancellationToken);

            if (request.BookingId.HasValue)
            {
                await EnsureUserCanReferenceBookingAsync(
                    userId,
                    request.BookingId.Value,
                    cancellationToken);
            }

            if (request.PropertyId.HasValue)
            {
                await EnsurePropertyExistsAsync(
                    request.PropertyId.Value,
                    cancellationToken);
            }

            var currentTime = DateTimeOffset.UtcNow;

            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = userId,
                BookingId = request.BookingId,
                PropertyId = request.PropertyId,
                Subject = subject,
                Description = description,
                Category = category,
                Urgency = urgency,
                Status = SupportTicketStatus.Open,
                DecisionStatus = SupportTicketDecisionStatus.NoDecision,
                DecisionAction = SupportTicketDecisionAction.NoAction,
                DecisionNote = null,
                DecidedAt = null,
                DecidedByAdminId = null,
                CreatedAt = currentTime,
                UpdatedAt = currentTime,
                ResolvedAt = null,
                ResolvedByAdminId = null,
                ResolutionNote = null
            };

            ticket.Messages.Add(new SupportTicketMessage
            {
                Id = Guid.NewGuid(),
                SupportTicketId = ticket.Id,
                SenderUserId = userId,
                Message = description,
                IsAdminMessage = false,
                CreatedAt = currentTime
            });

            await _dbContext.SupportTickets.AddAsync(ticket, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await TryHoldPayoutForComplaintAsync(ticket, cancellationToken);

            return await GetMyTicketByIdAsync(
                userId,
                ticket.Id,
                cancellationToken);
        }

        public async Task<SupportTicketsResponse> GetMyTicketsAsync(
            Guid userId,
            SupportTicketSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(userId);
            ArgumentNullException.ThrowIfNull(request);

            var query = _dbContext.SupportTickets
                .AsNoTracking()
                .Where(ticket => ticket.CreatedByUserId == userId);

            query = ApplyFilters(query, request);

            return await GetTicketsPageAsync(
                query,
                request,
                cancellationToken);
        }

        public async Task<SupportTicketResponse> GetMyTicketByIdAsync(
            Guid userId,
            Guid ticketId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(userId);
            ValidateTicketIdentifier(ticketId);

            var ticket = await GetTicketDetailsQuery()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == ticketId &&
                        item.CreatedByUserId == userId,
                    cancellationToken);

            if (ticket is null)
            {
                throw new KeyNotFoundException(
                    "The support ticket was not found.");
            }

            return MapTicketDetails(ticket);
        }

        public async Task<SupportTicketResponse> AddUserMessageAsync(
            Guid userId,
            Guid ticketId,
            CreateSupportTicketMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(userId);
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(request);

            var messageText = NormalizeRequiredText(
                request.Message,
                "The message is required.",
                4000);

            var ticket = await _dbContext.SupportTickets
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == ticketId &&
                        item.CreatedByUserId == userId,
                    cancellationToken);

            if (ticket is null)
            {
                throw new KeyNotFoundException(
                    "The support ticket was not found.");
            }

            EnsureTicketAcceptsUserUpdates(ticket);

            var currentTime = DateTimeOffset.UtcNow;

            await _dbContext.SupportTicketMessages.AddAsync(
                new SupportTicketMessage
                {
                    Id = Guid.NewGuid(),
                    SupportTicketId = ticket.Id,
                    SenderUserId = userId,
                    Message = messageText,
                    IsAdminMessage = false,
                    CreatedAt = currentTime
                },
                cancellationToken);

            ticket.Status = SupportTicketStatus.Open;
            ticket.UpdatedAt = currentTime;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetMyTicketByIdAsync(
                userId,
                ticket.Id,
                cancellationToken);
        }

        public async Task<UploadSupportTicketAttachmentResponse>
            UploadUserAttachmentAsync(
                Guid userId,
                Guid ticketId,
                IFormFile file,
                string? type,
                CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(userId);
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(file);

            ValidateAttachmentFile(file);

            var attachmentType = ParseAttachmentType(type);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var ticket = await _dbContext.SupportTickets
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == ticketId &&
                        item.CreatedByUserId == userId,
                    cancellationToken);

            if (ticket is null)
            {
                throw new KeyNotFoundException(
                    "The support ticket was not found.");
            }

            EnsureTicketAcceptsUserUpdates(ticket);

            var folder = BuildSupportTicketAttachmentFolder(ticket.Id);

            ImageUploadResult uploadResult;

            await using (var fileStream = file.OpenReadStream())
            {
                uploadResult = await _imageStorageService.UploadAsync(
                    fileStream,
                    file.FileName,
                    file.ContentType,
                    folder,
                    ImageAccessType.Public,
                    cancellationToken);
            }

            var currentTime = DateTimeOffset.UtcNow;

            var attachment = new SupportTicketAttachment
            {
                Id = Guid.NewGuid(),
                SupportTicketId = ticket.Id,
                UploadedByUserId = userId,
                Type = attachmentType,
                Url = uploadResult.SecureUrl,
                PublicId = uploadResult.PublicId,
                FileName = NormalizeFileName(file.FileName),
                ContentType = file.ContentType,
                FileSizeInBytes = file.Length,
                CreatedAt = currentTime
            };

            ticket.Status = SupportTicketStatus.Open;
            ticket.UpdatedAt = currentTime;

            try
            {
                await _dbContext.SupportTicketAttachments.AddAsync(
                    attachment,
                    cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _imageStorageService.DeleteAsync(
                    uploadResult.PublicId,
                    ImageAccessType.Public,
                    CancellationToken.None);

                throw;
            }

            return new UploadSupportTicketAttachmentResponse
            {
                TicketId = ticket.Id,
                AttachmentId = attachment.Id,
                Type = attachment.Type.ToString(),
                Url = attachment.Url,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSizeInBytes = attachment.FileSizeInBytes,
                CreatedAt = attachment.CreatedAt,
                Message =
                    "The support ticket evidence was uploaded successfully."
            };
        }

        // =====================================================
        // Admin operations
        // =====================================================

        public async Task<SupportTicketsResponse> GetAdminTicketsAsync(
            SupportTicketSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var query = ApplyFilters(
                _dbContext.SupportTickets.AsNoTracking(),
                request);

            return await GetTicketsPageAsync(
                query,
                request,
                cancellationToken);
        }

        public async Task<SupportTicketResponse> GetAdminTicketByIdAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default)
        {
            ValidateTicketIdentifier(ticketId);

            var ticket = await GetTicketDetailsQuery()
                .SingleOrDefaultAsync(
                    item => item.Id == ticketId,
                    cancellationToken);

            if (ticket is null)
            {
                throw new KeyNotFoundException(
                    "The support ticket was not found.");
            }

            return MapTicketDetails(ticket);
        }

        public async Task<SupportTicketResponse> AddAdminReplyAsync(
            Guid adminUserId,
            Guid ticketId,
            CreateSupportTicketMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(adminUserId);
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(request);

            var messageText = NormalizeRequiredText(
                request.Message,
                "The message is required.",
                4000);

            await EnsureActiveAdminUserAsync(
                adminUserId,
                cancellationToken);

            var ticket = await _dbContext.SupportTickets
                .SingleOrDefaultAsync(
                    item => item.Id == ticketId,
                    cancellationToken);

            if (ticket is null)
            {
                throw new KeyNotFoundException(
                    "The support ticket was not found.");
            }

            if (ticket.Status is
                SupportTicketStatus.Resolved or
                SupportTicketStatus.Closed)
            {
                throw new InvalidOperationException(
                    "You cannot reply to a resolved or closed support ticket.");
            }

            var currentTime = DateTimeOffset.UtcNow;

            await _dbContext.SupportTicketMessages.AddAsync(
                new SupportTicketMessage
                {
                    Id = Guid.NewGuid(),
                    SupportTicketId = ticket.Id,
                    SenderUserId = adminUserId,
                    Message = messageText,
                    IsAdminMessage = true,
                    CreatedAt = currentTime
                },
                cancellationToken);

            ticket.Status = SupportTicketStatus.InProgress;
            ticket.UpdatedAt = currentTime;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetAdminTicketByIdAsync(
                ticket.Id,
                cancellationToken);
        }

        public async Task<SupportTicketResponse> ApplyAdminDecisionAsync(
            Guid adminUserId,
            Guid ticketId,
            ApplySupportTicketDecisionRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(adminUserId);
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(request);

            var decisionStatus =
                ParseDecisionStatus(request.DecisionStatus);

            if (decisionStatus == SupportTicketDecisionStatus.NoDecision)
            {
                throw new ArgumentException(
                    "A real support ticket decision must be selected.");
            }

            var decisionAction =
                ParseDecisionAction(request.DecisionAction);

            var decisionNote =
                NormalizeOptionalText(request.DecisionNote, 1000);

            var adminMessageText =
                NormalizeOptionalText(request.AdminMessage, 4000);

            await EnsureActiveAdminUserAsync(
                adminUserId,
                cancellationToken);

            var ticket = await _dbContext.SupportTickets
                .SingleOrDefaultAsync(
                    item => item.Id == ticketId,
                    cancellationToken);

            if (ticket is null)
            {
                throw new KeyNotFoundException(
                    "The support ticket was not found.");
            }

            if (ticket.Status == SupportTicketStatus.Closed)
            {
                throw new InvalidOperationException(
                    "Closed tickets cannot be changed.");
            }

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            var currentTime = DateTimeOffset.UtcNow;

            ticket.DecisionStatus = decisionStatus;
            ticket.DecisionAction = decisionAction;
            ticket.DecisionNote = decisionNote;
            ticket.DecidedAt = currentTime;
            ticket.DecidedByAdminId = adminUserId;
            ticket.UpdatedAt = currentTime;

            if (request.ResolveTicket)
            {
                ticket.Status = SupportTicketStatus.Resolved;
                ticket.ResolvedAt = currentTime;
                ticket.ResolvedByAdminId = adminUserId;
                ticket.ResolutionNote = decisionNote;
            }
            else if (
                decisionStatus ==
                SupportTicketDecisionStatus.NeedsMoreEvidence)
            {
                ticket.Status = SupportTicketStatus.Open;
                ticket.ResolvedAt = null;
                ticket.ResolvedByAdminId = null;
                ticket.ResolutionNote = null;
            }
            else
            {
                ticket.Status = SupportTicketStatus.InProgress;
                ticket.ResolvedAt = null;
                ticket.ResolvedByAdminId = null;
                ticket.ResolutionNote = null;
            }

            if (!string.IsNullOrWhiteSpace(adminMessageText))
            {
                await _dbContext.SupportTicketMessages.AddAsync(
                    new SupportTicketMessage
                    {
                        Id = Guid.NewGuid(),
                        SupportTicketId = ticket.Id,
                        SenderUserId = adminUserId,
                        Message = adminMessageText,
                        IsAdminMessage = true,
                        CreatedAt = currentTime
                    },
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await ApplyPayoutActionForDecisionAsync(
                ticket,
                decisionAction,
                decisionNote,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await GetAdminTicketByIdAsync(
                ticket.Id,
                cancellationToken);
        }

        public async Task<SupportTicketResponse> ResolveTicketAsync(
            Guid adminUserId,
            Guid ticketId,
            ResolveSupportTicketRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(adminUserId);
            ValidateTicketIdentifier(ticketId);
            ArgumentNullException.ThrowIfNull(request);

            var resolutionNote =
                NormalizeOptionalText(request.ResolutionNote, 1000);

            await EnsureActiveAdminUserAsync(
                adminUserId,
                cancellationToken);

            var ticket = await _dbContext.SupportTickets
                .SingleOrDefaultAsync(
                    item => item.Id == ticketId,
                    cancellationToken);

            if (ticket is null)
            {
                throw new KeyNotFoundException(
                    "The support ticket was not found.");
            }

            if (ticket.Status == SupportTicketStatus.Closed)
            {
                throw new InvalidOperationException(
                    "Closed tickets cannot be resolved again.");
            }

            var currentTime = DateTimeOffset.UtcNow;

            ticket.Status = SupportTicketStatus.Resolved;
            ticket.ResolvedAt = currentTime;
            ticket.ResolvedByAdminId = adminUserId;
            ticket.ResolutionNote = resolutionNote;
            ticket.UpdatedAt = currentTime;

            if (!string.IsNullOrWhiteSpace(resolutionNote))
            {
                await _dbContext.SupportTicketMessages.AddAsync(
                    new SupportTicketMessage
                    {
                        Id = Guid.NewGuid(),
                        SupportTicketId = ticket.Id,
                        SenderUserId = adminUserId,
                        Message = resolutionNote,
                        IsAdminMessage = true,
                        CreatedAt = currentTime
                    },
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetAdminTicketByIdAsync(
                ticket.Id,
                cancellationToken);
        }

        // =====================================================
        // Querying and mapping
        // =====================================================

        private IQueryable<SupportTicket> GetTicketDetailsQuery()
        {
            return _dbContext.SupportTickets
                .AsNoTracking()
                .AsSplitQuery()
                .Include(ticket => ticket.CreatedByUser)
                .Include(ticket => ticket.Property)
                .Include(ticket => ticket.DecidedByAdmin)
                .Include(ticket => ticket.Messages)
                    .ThenInclude(message => message.SenderUser)
                .Include(ticket => ticket.Attachments)
                    .ThenInclude(attachment => attachment.UploadedByUser);
        }

        private async Task<SupportTicketsResponse> GetTicketsPageAsync(
            IQueryable<SupportTicket> query,
            SupportTicketSearchRequest request,
            CancellationToken cancellationToken)
        {
            var page = NormalizePage(request.Page);
            var pageSize = NormalizePageSize(request.PageSize);

            var totalCount =
                await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderByDescending(ticket => ticket.Urgency)
                .ThenByDescending(ticket => ticket.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ticket => new
                {
                    TicketId = ticket.Id,
                    ticket.Subject,
                    Category = ticket.Category,
                    Urgency = ticket.Urgency,
                    Status = ticket.Status,
                    ticket.CreatedByUserId,
                    CreatedByFirstName =
                        ticket.CreatedByUser.FirstName,
                    CreatedByLastName =
                        ticket.CreatedByUser.LastName,
                    CreatedByEmail =
                        ticket.CreatedByUser.Email,
                    ticket.BookingId,
                    ticket.PropertyId,
                    PropertyTitle =
                        ticket.Property == null
                            ? null
                            : ticket.Property.Title,
                    MessagesCount =
                        ticket.Messages.Count,
                    ticket.CreatedAt,
                    ticket.UpdatedAt,
                    ticket.ResolvedAt
                })
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(row => new SupportTicketListItemResponse
                {
                    TicketId = row.TicketId,
                    ReferenceCode =
                        BuildSupportTicketReferenceCode(row.TicketId),
                    Subject = row.Subject,
                    Category = row.Category.ToString(),
                    Urgency = row.Urgency.ToString(),
                    Status = row.Status.ToString(),
                    CreatedByUserId = row.CreatedByUserId,
                    CreatedByName = BuildFullName(
                        row.CreatedByFirstName,
                        row.CreatedByLastName,
                        row.CreatedByEmail),
                    CreatedByEmail = row.CreatedByEmail,
                    BookingId = row.BookingId,
                    PropertyId = row.PropertyId,
                    PropertyTitle = row.PropertyTitle,
                    MessagesCount = row.MessagesCount,
                    CreatedAt = row.CreatedAt,
                    UpdatedAt = row.UpdatedAt,
                    ResolvedAt = row.ResolvedAt
                })
                .ToList();

            return new SupportTicketsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages =
                    CalculateTotalPages(totalCount, pageSize),
                Items = items
            };
        }

        private static IQueryable<SupportTicket> ApplyFilters(
            IQueryable<SupportTicket> query,
            SupportTicketSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var likePattern =
                    $"%{request.Search.Trim()}%";

                query = query.Where(ticket =>
                    EF.Functions.Like(
                        ticket.Subject,
                        likePattern)
                    ||
                    EF.Functions.Like(
                        ticket.Description,
                        likePattern)
                    ||
                    (
                        ticket.CreatedByUser.Email != null
                        &&
                        EF.Functions.Like(
                            ticket.CreatedByUser.Email,
                            likePattern)
                    )
                    ||
                    (
                        ticket.Property != null
                        &&
                        EF.Functions.Like(
                            ticket.Property.Title,
                            likePattern)
                    ));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = ParseStatus(request.Status);

                query = query.Where(
                    ticket => ticket.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                var category = ParseCategory(request.Category);

                query = query.Where(
                    ticket => ticket.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(request.Urgency))
            {
                var urgency = ParseUrgency(request.Urgency);

                query = query.Where(
                    ticket => ticket.Urgency == urgency);
            }

            return query;
        }

        private static SupportTicketResponse MapTicketDetails(
            SupportTicket ticket)
        {
            return new SupportTicketResponse
            {
                TicketId = ticket.Id,
                ReferenceCode =
                    BuildSupportTicketReferenceCode(ticket.Id),
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByName = BuildFullName(
                    ticket.CreatedByUser.FirstName,
                    ticket.CreatedByUser.LastName,
                    ticket.CreatedByUser.Email),
                CreatedByEmail = ticket.CreatedByUser.Email,
                BookingId = ticket.BookingId,
                PropertyId = ticket.PropertyId,
                PropertyTitle = ticket.Property?.Title,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Category = ticket.Category.ToString(),
                Urgency = ticket.Urgency.ToString(),
                Status = ticket.Status.ToString(),
                DecisionStatus = ticket.DecisionStatus.ToString(),
                DecisionAction = ticket.DecisionAction.ToString(),
                DecisionNote = ticket.DecisionNote,
                DecidedAt = ticket.DecidedAt,
                DecidedByAdminId = ticket.DecidedByAdminId,
                DecidedByAdminName =
                    ticket.DecidedByAdmin is null
                        ? null
                        : BuildFullName(
                            ticket.DecidedByAdmin.FirstName,
                            ticket.DecidedByAdmin.LastName,
                            ticket.DecidedByAdmin.Email),
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt,
                ResolutionNote = ticket.ResolutionNote,

                Messages = ticket.Messages
                    .OrderBy(message => message.CreatedAt)
                    .Select(message =>
                        new SupportTicketMessageResponse
                        {
                            MessageId = message.Id,
                            SenderUserId = message.SenderUserId,
                            SenderName = BuildFullName(
                                message.SenderUser.FirstName,
                                message.SenderUser.LastName,
                                message.SenderUser.Email),
                            SenderEmail =
                                message.SenderUser.Email,
                            IsAdminMessage =
                                message.IsAdminMessage,
                            Message = message.Message,
                            CreatedAt = message.CreatedAt
                        })
                    .ToList(),

                Attachments = ticket.Attachments
                    .OrderBy(attachment => attachment.CreatedAt)
                    .Select(attachment =>
                        new SupportTicketAttachmentResponse
                        {
                            AttachmentId = attachment.Id,
                            UploadedByUserId =
                                attachment.UploadedByUserId,
                            UploadedByName = BuildFullName(
                                attachment.UploadedByUser.FirstName,
                                attachment.UploadedByUser.LastName,
                                attachment.UploadedByUser.Email),
                            UploadedByEmail =
                                attachment.UploadedByUser.Email,
                            Type = attachment.Type.ToString(),
                            Url = attachment.Url,
                            FileName = attachment.FileName,
                            ContentType = attachment.ContentType,
                            FileSizeInBytes =
                                attachment.FileSizeInBytes,
                            CreatedAt = attachment.CreatedAt
                        })
                    .ToList()
            };
        }

        // =====================================================
        // Payout helpers
        // =====================================================

        private async Task TryHoldPayoutForComplaintAsync(
            SupportTicket ticket,
            CancellationToken cancellationToken)
        {
            if (!ShouldHoldPayoutForSupportTicket(ticket))
            {
                return;
            }

            var holdReason =
                $"Support ticket {BuildSupportTicketReferenceCode(ticket.Id)} was opened for this booking.";

            try
            {
                await _bookingPayoutService
                    .HoldPayoutForBookingAsync(
                        ticket.BookingId!.Value,
                        holdReason,
                        cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                /*
                 * Older bookings might not contain
                 * a payout record.
                 */
            }
            catch (InvalidOperationException)
            {
                /*
                 * Paid, refunded, or blocked payouts
                 * cannot be placed on hold.
                 */
            }
        }

        private async Task ApplyPayoutActionForDecisionAsync(
            SupportTicket ticket,
            SupportTicketDecisionAction decisionAction,
            string? decisionNote,
            CancellationToken cancellationToken)
        {
            var requiresBooking =
                decisionAction is
                    SupportTicketDecisionAction.HoldPayoutRecommended
                    or SupportTicketDecisionAction.ReleasePayoutRecommended
                    or SupportTicketDecisionAction.PartialRefundRecommended
                    or SupportTicketDecisionAction.FullRefundRecommended;

            if (!ticket.BookingId.HasValue)
            {
                if (requiresBooking)
                {
                    throw new InvalidOperationException(
                        "The selected support decision requires a ticket linked to a booking.");
                }

                return;
            }

            switch (decisionAction)
            {
                case SupportTicketDecisionAction.HoldPayoutRecommended:
                    await _bookingPayoutService
                        .HoldPayoutForBookingAsync(
                            ticket.BookingId.Value,
                            decisionNote
                            ??
                            $"Support ticket {BuildSupportTicketReferenceCode(ticket.Id)} requires payout hold.",
                            cancellationToken);
                    break;

                case SupportTicketDecisionAction.ReleasePayoutRecommended:
                    await _bookingPayoutService
                        .ReleasePayoutForBookingAsync(
                            ticket.BookingId.Value,
                            decisionNote,
                            cancellationToken);
                    break;

                case SupportTicketDecisionAction.PartialRefundRecommended:
                case SupportTicketDecisionAction.FullRefundRecommended:
                    await _bookingPayoutService
                        .BlockPayoutForBookingAsync(
                            ticket.BookingId.Value,
                            decisionNote
                            ??
                            $"Support ticket {BuildSupportTicketReferenceCode(ticket.Id)} has a refund recommendation.",
                            cancellationToken);
                    break;
            }
        }

        private static bool ShouldHoldPayoutForSupportTicket(
            SupportTicket ticket)
        {
            return ticket.BookingId.HasValue
                &&
                ticket.Category is
                    SupportTicketCategory.BookingIssue
                    or SupportTicketCategory.PaymentIssue
                    or SupportTicketCategory.PropertyIssue
                    or SupportTicketCategory.HostIssue
                    or SupportTicketCategory.RefundIssue;
        }

        // =====================================================
        // Validation helpers
        // =====================================================

        private async Task EnsureActiveUserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var exists = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    user =>
                        user.Id == userId &&
                        user.IsActive,
                    cancellationToken);

            if (!exists)
            {
                throw new UnauthorizedAccessException(
                    "The user was not found or is inactive.");
            }
        }

        private async Task EnsureActiveAdminUserAsync(
            Guid adminUserId,
            CancellationToken cancellationToken)
        {
            var adminUser = await _dbContext.Users
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == adminUserId &&
                        user.IsActive,
                    cancellationToken);

            if (adminUser is null)
            {
                throw new UnauthorizedAccessException(
                    "The admin user was not found or is inactive.");
            }

            var isAdmin =
                await _userManager.IsInRoleAsync(
                    adminUser,
                    RoleNames.Admin);

            if (!isAdmin)
            {
                throw new UnauthorizedAccessException(
                    "Only admins can perform this support action.");
            }
        }

        private async Task EnsureUserCanReferenceBookingAsync(
            Guid userId,
            Guid bookingId,
            CancellationToken cancellationToken)
        {
            if (bookingId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The booking identifier is invalid.");
            }

            var canReferenceBooking =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .AnyAsync(
                        booking =>
                            booking.Id == bookingId
                            &&
                            (
                                booking.GuestUserId == userId
                                ||
                                booking.Property
                                    .HostProfile
                                    .UserId == userId
                            ),
                        cancellationToken);

            if (!canReferenceBooking)
            {
                throw new InvalidOperationException(
                    "The booking was not found or does not belong to the current user.");
            }
        }

        private async Task EnsurePropertyExistsAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
        {
            if (propertyId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The property identifier is invalid.");
            }

            var exists = await _dbContext.Properties
                .AsNoTracking()
                .AnyAsync(
                    property =>
                        property.Id == propertyId,
                    cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(
                    "The referenced property was not found.");
            }
        }

        private static void EnsureTicketAcceptsUserUpdates(
            SupportTicket ticket)
        {
            if (ticket.Status is
                SupportTicketStatus.Resolved or
                SupportTicketStatus.Closed)
            {
                throw new InvalidOperationException(
                    "You cannot update a resolved or closed support ticket.");
            }
        }

        private static void ValidateUserIdentifier(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain a valid user identifier.");
            }
        }

        private static void ValidateTicketIdentifier(Guid ticketId)
        {
            if (ticketId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The support ticket identifier is invalid.");
            }
        }

        private static void ValidateAttachmentFile(IFormFile file)
        {
            if (file.Length <= 0)
            {
                throw new ArgumentException(
                    "The uploaded evidence image is empty.");
            }

            if (file.Length > MaximumAttachmentSizeInBytes)
            {
                throw new ArgumentException(
                    "The evidence image size must not exceed 5 MB.");
            }

            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                throw new ArgumentException(
                    "The uploaded file name is required.");
            }

            if (string.IsNullOrWhiteSpace(file.ContentType))
            {
                throw new ArgumentException(
                    "The uploaded file content type is required.");
            }

            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (!AllowedImageContentTypes.TryGetValue(
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

        private static string NormalizeFileName(string fileName)
        {
            var normalizedFileName =
                Path.GetFileName(fileName.Trim());

            if (string.IsNullOrWhiteSpace(normalizedFileName))
            {
                throw new ArgumentException(
                    "The uploaded file name is invalid.");
            }

            return normalizedFileName;
        }

        private static string NormalizeRequiredText(
            string? value,
            string errorMessage,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(errorMessage);
            }

            var normalizedValue = value.Trim();

            if (normalizedValue.Length > maximumLength)
            {
                throw new ArgumentException(
                    $"The value cannot exceed {maximumLength} characters.");
            }

            return normalizedValue;
        }

        private static string? NormalizeOptionalText(
            string? value,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalizedValue = value.Trim();

            if (normalizedValue.Length > maximumLength)
            {
                throw new ArgumentException(
                    $"The value cannot exceed {maximumLength} characters.");
            }

            return normalizedValue;
        }

        private static SupportTicketStatus ParseStatus(string value)
        {
            if (Enum.TryParse<SupportTicketStatus>(
                    value,
                    ignoreCase: true,
                    out var status)
                &&
                Enum.IsDefined(status))
            {
                return status;
            }

            throw new ArgumentException(
                "The support ticket status is invalid.");
        }

        private static SupportTicketCategory ParseCategory(string value)
        {
            if (Enum.TryParse<SupportTicketCategory>(
                    value,
                    ignoreCase: true,
                    out var category)
                &&
                Enum.IsDefined(category))
            {
                return category;
            }

            throw new ArgumentException(
                "The support ticket category is invalid.");
        }

        private static SupportTicketUrgency ParseUrgency(string value)
        {
            if (Enum.TryParse<SupportTicketUrgency>(
                    value,
                    ignoreCase: true,
                    out var urgency)
                &&
                Enum.IsDefined(urgency))
            {
                return urgency;
            }

            throw new ArgumentException(
                "The support ticket urgency is invalid.");
        }

        private static SupportTicketAttachmentType ParseAttachmentType(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return SupportTicketAttachmentType.IssueEvidence;
            }

            var normalizedValue = value.Trim();

            if (
                int.TryParse(
                    normalizedValue,
                    out var numericValue)
                &&
                Enum.IsDefined(
                    typeof(SupportTicketAttachmentType),
                    numericValue)
            )
            {
                return (SupportTicketAttachmentType)numericValue;
            }

            if (
                Enum.TryParse<SupportTicketAttachmentType>(
                    normalizedValue,
                    ignoreCase: true,
                    out var attachmentType)
                &&
                Enum.IsDefined(attachmentType)
            )
            {
                return attachmentType;
            }

            var normalizedToken =
                NormalizeEnumToken(normalizedValue);

            return normalizedToken switch
            {
                "propertyphoto"
                or "propertyimage"
                or "listingphoto"
                or "listingimage" =>
                    SupportTicketAttachmentType.PropertyPhoto,

                "selfieatproperty"
                or "propertyselfie"
                or "selfie" =>
                    SupportTicketAttachmentType.SelfieAtProperty,

                "issueevidence"
                or "evidence"
                or "proof"
                or "image"
                or "photo"
                or "picture"
                or "screenshot" =>
                    SupportTicketAttachmentType.IssueEvidence,

                "paymentevidence"
                or "paymentproof"
                or "payment"
                or "receipt"
                or "refundproof"
                or "refundevidence" =>
                    SupportTicketAttachmentType.PaymentEvidence,

                "other"
                or "otherimage"
                or "otherevidence" =>
                    SupportTicketAttachmentType.Other,

                _ =>
                    throw new ArgumentException(
                        "The support ticket attachment type is invalid.")
            };
        }

        private static SupportTicketDecisionStatus ParseDecisionStatus(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "The support ticket decision status is invalid.");
            }

            var normalizedValue = value.Trim();

            if (
                int.TryParse(
                    normalizedValue,
                    out var numericValue)
                &&
                Enum.IsDefined(
                    typeof(SupportTicketDecisionStatus),
                    numericValue)
            )
            {
                return (SupportTicketDecisionStatus)numericValue;
            }

            if (
                Enum.TryParse<SupportTicketDecisionStatus>(
                    normalizedValue,
                    ignoreCase: true,
                    out var decisionStatus)
                &&
                Enum.IsDefined(decisionStatus)
            )
            {
                return decisionStatus;
            }

            var normalizedToken =
                NormalizeEnumToken(normalizedValue);

            var mappedStatus =
                normalizedToken switch
                {
                    "nodecision"
                    or "pending"
                    or "none" =>
                        SupportTicketDecisionStatus.NoDecision,

                    "validcomplaint"
                    or "guestclaimaccepted"
                    or "claimaccepted"
                    or "accepted"
                    or "accept"
                    or "valid" =>
                        SupportTicketDecisionStatus.ValidComplaint,

                    "invalidcomplaint"
                    or "guestclaimrejected"
                    or "claimrejected"
                    or "rejected"
                    or "reject"
                    or "invalid" =>
                        SupportTicketDecisionStatus.InvalidComplaint,

                    "needsmoreevidence"
                    or "moreevidence"
                    or "needevidence"
                    or "evidenceneeded" =>
                        SupportTicketDecisionStatus.NeedsMoreEvidence,

                    _ =>
                        (SupportTicketDecisionStatus?)null
                };

            return mappedStatus
                ??
                throw new ArgumentException(
                    "The support ticket decision status is invalid.");
        }

        private static SupportTicketDecisionAction ParseDecisionAction(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "The support ticket decision action is invalid.");
            }

            var normalizedValue = value.Trim();

            if (
                int.TryParse(
                    normalizedValue,
                    out var numericValue)
                &&
                Enum.IsDefined(
                    typeof(SupportTicketDecisionAction),
                    numericValue)
            )
            {
                return (SupportTicketDecisionAction)numericValue;
            }

            if (
                Enum.TryParse<SupportTicketDecisionAction>(
                    normalizedValue,
                    ignoreCase: true,
                    out var decisionAction)
                &&
                Enum.IsDefined(decisionAction)
            )
            {
                return decisionAction;
            }

            var normalizedToken =
                NormalizeEnumToken(normalizedValue);

            var mappedAction =
                normalizedToken switch
                {
                    "noaction"
                    or "noactionyet"
                    or "none" =>
                        SupportTicketDecisionAction.NoAction,

                    "partialrefundrecommended"
                    or "partialrefund" =>
                        SupportTicketDecisionAction
                            .PartialRefundRecommended,

                    "fullrefundrecommended"
                    or "fullrefund" =>
                        SupportTicketDecisionAction
                            .FullRefundRecommended,

                    "hostwarningrecommended"
                    or "hostwarning"
                    or "warnhost" =>
                        SupportTicketDecisionAction
                            .HostWarningRecommended,

                    "hidepropertyrecommended"
                    or "hideproperty"
                    or "propertyhide" =>
                        SupportTicketDecisionAction
                            .HidePropertyRecommended,

                    "holdpayoutrecommended"
                    or "holdhostpayout"
                    or "holdpayout" =>
                        SupportTicketDecisionAction
                            .HoldPayoutRecommended,

                    "releasepayoutrecommended"
                    or "releasehostpayout"
                    or "releasepayout" =>
                        SupportTicketDecisionAction
                            .ReleasePayoutRecommended,

                    _ =>
                        (SupportTicketDecisionAction?)null
                };

            return mappedAction
                ??
                throw new ArgumentException(
                    "The support ticket decision action is invalid.");
        }

        private static string NormalizeEnumToken(string value)
        {
            return new string(
                value
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToLowerInvariant)
                    .ToArray());
        }

        private static int NormalizePage(int page)
        {
            return page <= 0
                ? 1
                : page;
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize <= 0)
            {
                return 20;
            }

            return Math.Min(
                pageSize,
                MaximumPageSize);
        }

        private static int CalculateTotalPages(
            int totalCount,
            int pageSize)
        {
            return totalCount <= 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);
        }

        private string BuildSupportTicketAttachmentFolder(
            Guid ticketId)
        {
            var baseFolder =
                (_cloudinarySettings.BaseFolder ?? string.Empty)
                    .Trim()
                    .Trim('/');

            var ticketFolder =
                $"support-tickets/{ticketId}/attachments";

            return string.IsNullOrWhiteSpace(baseFolder)
                ? ticketFolder
                : $"{baseFolder}/{ticketFolder}";
        }

        private static string BuildSupportTicketReferenceCode(
            Guid ticketId)
        {
            var normalizedId =
                ticketId.ToString("N");

            return
                $"ST-{normalizedId.Substring(0, 4).ToUpperInvariant()}";
        }

        private static string BuildFullName(
            string? firstName,
            string? lastName,
            string? fallback)
        {
            var fullName = string.Join(
                " ",
                new[]
                {
                    firstName,
                    lastName
                }
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(value))
                .Select(
                    value =>
                        value!.Trim()));

            return !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : fallback ?? "Unknown User";
        }
    }
}