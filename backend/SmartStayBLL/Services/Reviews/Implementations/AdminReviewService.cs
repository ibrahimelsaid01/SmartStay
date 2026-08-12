using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminReviewService
        : IAdminReviewService
    {
        private const int MaximumPageSize = 100;

        private const int MaximumRejectionReasonLength =
            500;

        private readonly SmartStayDbContext _dbContext;

        public AdminReviewService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext = dbContext;
        }

        public async Task<AdminReviewsResponse>
            GetReviewsAsync(
                ReviewStatus? status,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            ValidatePagination(
                page,
                pageSize);

            ValidateOptionalStatus(
                status);

            var query =
                BuildAdminReviewsQuery();

            if (status.HasValue)
            {
                query =
                    query.Where(review =>
                        review.Status ==
                            status.Value);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var reviews =
                await query
                    .OrderByDescending(review =>
                        review.CreatedAt)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(
                        cancellationToken);

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount
                        /
                        (double)pageSize);

            return new AdminReviewsResponse
            {
                Items =
                    reviews
                        .Select(MapListItem)
                        .ToList(),

                Page =
                    page,

                PageSize =
                    pageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    totalPages
            };
        }

        public async Task<AdminReviewDetailsResponse>
            GetByIdAsync(
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            ValidateReviewId(
                reviewId);

            var review =
                await BuildAdminReviewsQuery()
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == reviewId,
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The review was not found.");
            }

            return MapDetails(
                review);
        }

        public async Task<AdminReviewModerationResponse>
            ApproveAsync(
                Guid adminUserId,
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                adminUserId);

            ValidateReviewId(
                reviewId);

            await EnsureActiveAdminExistsAsync(
                adminUserId,
                cancellationToken);

            var review =
                await _dbContext.Reviews
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == reviewId,
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The review was not found.");
            }

            if (review.Status !=
                ReviewStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending reviews can be approved.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            review.Status =
                ReviewStatus.Posted;

            review.ModeratedByUserId =
                adminUserId;

            review.ModeratedAt =
                currentTime;

            review.PublishedAt =
                currentTime;

            review.RejectedAt =
                null;

            review.RejectionReason =
                null;

            review.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new AdminReviewModerationResponse
            {
                Id =
                    review.Id,

                Status =
                    review.Status.ToString(),

                ModeratedAt =
                    currentTime,

                PublishedAt =
                    currentTime,

                RejectedAt =
                    null,

                RejectionReason =
                    null,

                Message =
                    "The review was approved and published successfully."
            };
        }

        public async Task<AdminReviewModerationResponse>
            RejectAsync(
                Guid adminUserId,
                Guid reviewId,
                RejectReviewRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(
                adminUserId);

            ValidateReviewId(
                reviewId);

            ArgumentNullException.ThrowIfNull(
                request);

            var reason =
                NormalizeRejectionReason(
                    request.Reason);

            await EnsureActiveAdminExistsAsync(
                adminUserId,
                cancellationToken);

            var review =
                await _dbContext.Reviews
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == reviewId,
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The review was not found.");
            }

            if (review.Status !=
                ReviewStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending reviews can be rejected.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            review.Status =
                ReviewStatus.Rejected;

            review.ModeratedByUserId =
                adminUserId;

            review.ModeratedAt =
                currentTime;

            review.PublishedAt =
                null;

            review.RejectedAt =
                currentTime;

            review.RejectionReason =
                reason;

            review.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new AdminReviewModerationResponse
            {
                Id =
                    review.Id,

                Status =
                    review.Status.ToString(),

                ModeratedAt =
                    currentTime,

                PublishedAt =
                    null,

                RejectedAt =
                    currentTime,

                RejectionReason =
                    reason,

                Message =
                    "The review was rejected successfully."
            };
        }

        private IQueryable<AdminReviewProjection>
            BuildAdminReviewsQuery()
        {
            return _dbContext.Reviews
                .AsNoTracking()
                .Select(review =>
                    new AdminReviewProjection
                    {
                        Id =
                            review.Id,

                        BookingId =
                            review.BookingId,

                        PropertyId =
                            review.PropertyId,

                        Rating =
                            review.Rating,

                        PositiveComment =
                            review.PositiveComment,

                        NegativeComment =
                            review.NegativeComment,

                        Status =
                            review.Status,

                        RejectionReason =
                            review.RejectionReason,

                        CheckInDate =
                            review.Booking.CheckInDate,

                        CheckOutDate =
                            review.Booking.CheckOutDate,

                        HelpfulCount =
                            review.HelpfulVotes.Count,

                        AuthorUserId =
                            review.UserId,

                        AuthorFirstName =
                            review.User.FirstName,

                        AuthorLastName =
                            review.User.LastName,

                        AuthorProfileImageUrl =
                            review.User.ProfileImageUrl,

                        PropertyTitle =
                            review.Property.Title,

                        PropertyCountry =
                            review.Property.Country,

                        PropertyCity =
                            review.Property.City,

                        PropertyCoverImageUrl =
                            review.Property.Images
                                .OrderByDescending(image =>
                                    image.IsCover)
                                .ThenBy(image =>
                                    image.DisplayOrder)
                                .Select(image =>
                                    image.Url)
                                .FirstOrDefault(),

                        CreatedAt =
                            review.CreatedAt,

                        UpdatedAt =
                            review.UpdatedAt,

                        ModeratedAt =
                            review.ModeratedAt,

                        PublishedAt =
                            review.PublishedAt,

                        RejectedAt =
                            review.RejectedAt,

                        ReplyId =
                            review.Reply == null
                                ? null
                                : review.Reply.Id,

                        ReplyHostProfileId =
                            review.Reply == null
                                ? null
                                : review.Reply.HostProfileId,

                        ReplyHostDisplayName =
                            review.Reply == null
                                ? null
                                : review.Reply.HostProfile
                                    .DisplayName,

                        ReplyHostProfileImageUrl =
                            review.Reply == null
                                ? null
                                : review.Reply.HostProfile
                                    .ProfileImageUrl,

                        ReplyContent =
                            review.Reply == null
                                ? null
                                : review.Reply.Content,

                        ReplyCreatedAt =
                            review.Reply == null
                                ? null
                                : review.Reply.CreatedAt,

                        ReplyUpdatedAt =
                            review.Reply == null
                                ? null
                                : review.Reply.UpdatedAt
                    });
        }

        private static AdminReviewListItemResponse
            MapListItem(
                AdminReviewProjection review)
        {
            return new AdminReviewListItemResponse
            {
                Id =
                    review.Id,

                BookingId =
                    review.BookingId,

                Rating =
                    review.Rating,

                Status =
                    review.Status.ToString(),

                Author =
                    MapAuthor(review),

                Property =
                    MapProperty(review),

                CreatedAt =
                    review.CreatedAt,

                UpdatedAt =
                    review.UpdatedAt,

                PublishedAt =
                    review.PublishedAt,

                RejectedAt =
                    review.RejectedAt
            };
        }

        private static AdminReviewDetailsResponse
            MapDetails(
                AdminReviewProjection review)
        {
            return new AdminReviewDetailsResponse
            {
                Id =
                    review.Id,

                BookingId =
                    review.BookingId,

                Rating =
                    review.Rating,

                PositiveComment =
                    review.PositiveComment,

                NegativeComment =
                    review.NegativeComment,

                Status =
                    review.Status.ToString(),

                RejectionReason =
                    review.RejectionReason,

                CheckInDate =
                    review.CheckInDate,

                CheckOutDate =
                    review.CheckOutDate,

                HelpfulCount =
                    review.HelpfulCount,

                Author =
                    MapAuthor(review),

                Property =
                    MapProperty(review),

                Reply =
                    MapReply(review),

                CreatedAt =
                    review.CreatedAt,

                UpdatedAt =
                    review.UpdatedAt,

                ModeratedAt =
                    review.ModeratedAt,

                PublishedAt =
                    review.PublishedAt,

                RejectedAt =
                    review.RejectedAt
            };
        }

        private static ReviewAuthorResponse MapAuthor(
            AdminReviewProjection review)
        {
            return new ReviewAuthorResponse
            {
                UserId =
                    review.AuthorUserId,

                DisplayName =
                    BuildAuthorDisplayName(
                        review.AuthorFirstName,
                        review.AuthorLastName),

                ProfileImageUrl =
                    review.AuthorProfileImageUrl
            };
        }

        private static ReviewPropertyResponse MapProperty(
            AdminReviewProjection review)
        {
            return new ReviewPropertyResponse
            {
                Id =
                    review.PropertyId,

                Title =
                    review.PropertyTitle,

                Country =
                    review.PropertyCountry
                    ?? string.Empty,

                City =
                    review.PropertyCity
                    ?? string.Empty,

                CoverImageUrl =
                    review.PropertyCoverImageUrl
            };
        }

        private static ReviewReplyResponse? MapReply(
            AdminReviewProjection review)
        {
            if (!review.ReplyId.HasValue
                ||
                !review.ReplyHostProfileId.HasValue
                ||
                !review.ReplyCreatedAt.HasValue
                ||
                string.IsNullOrWhiteSpace(
                    review.ReplyContent))
            {
                return null;
            }

            return new ReviewReplyResponse
            {
                Id =
                    review.ReplyId.Value,

                HostProfileId =
                    review.ReplyHostProfileId.Value,

                HostDisplayName =
                    review.ReplyHostDisplayName
                    ?? string.Empty,

                HostProfileImageUrl =
                    review.ReplyHostProfileImageUrl,

                Content =
                    review.ReplyContent,

                CreatedAt =
                    review.ReplyCreatedAt.Value,

                UpdatedAt =
                    review.ReplyUpdatedAt
            };
        }

        private async Task EnsureActiveAdminExistsAsync(
            Guid adminUserId,
            CancellationToken cancellationToken)
        {
            var adminExists =
                await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id ==
                                adminUserId
                            &&
                            user.IsActive,
                        cancellationToken);

            if (!adminExists)
            {
                throw new UnauthorizedAccessException(
                    "The administrator account is inactive or was not found.");
            }
        }

        private static string NormalizeRejectionReason(
            string? reason)
        {
            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                throw new ArgumentException(
                    "The rejection reason is required.");
            }

            var normalizedReason =
                reason.Trim();

            if (normalizedReason.Length is < 3
                or > MaximumRejectionReasonLength)
            {
                throw new ArgumentException(
                    $"The rejection reason must be between 3 " +
                    $"and {MaximumRejectionReasonLength} characters.");
            }

            return normalizedReason;
        }

        private static string BuildAuthorDisplayName(
            string? firstName,
            string? lastName)
        {
            var normalizedFirstName =
                string.IsNullOrWhiteSpace(
                    firstName)
                    ? "SmartStay guest"
                    : firstName.Trim();

            if (string.IsNullOrWhiteSpace(
                    lastName))
            {
                return normalizedFirstName;
            }

            return
                $"{normalizedFirstName} " +
                $"{char.ToUpperInvariant(lastName.Trim()[0])}.";
        }

        private static void ValidateUserId(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The administrator identifier is invalid.");
            }
        }

        private static void ValidateReviewId(
            Guid reviewId)
        {
            if (reviewId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The review identifier is invalid.");
            }
        }

        private static void ValidateOptionalStatus(
            ReviewStatus? status)
        {
            if (status.HasValue
                &&
                !Enum.IsDefined(
                    typeof(ReviewStatus),
                    status.Value))
            {
                throw new ArgumentException(
                    "The review status is invalid.");
            }
        }

        private static void ValidatePagination(
            int page,
            int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentException(
                    "Page must be greater than or equal to 1.");
            }

            if (pageSize < 1
                ||
                pageSize > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"Page size must be between 1 and " +
                    $"{MaximumPageSize}.");
            }
        }

        private sealed class AdminReviewProjection
        {
            public Guid Id { get; set; }

            public Guid BookingId { get; set; }

            public Guid PropertyId { get; set; }

            public int Rating { get; set; }

            public string? PositiveComment { get; set; }

            public string? NegativeComment { get; set; }

            public ReviewStatus Status { get; set; }

            public string? RejectionReason { get; set; }

            public DateOnly CheckInDate { get; set; }

            public DateOnly CheckOutDate { get; set; }

            public int HelpfulCount { get; set; }

            public Guid AuthorUserId { get; set; }

            public string? AuthorFirstName { get; set; }

            public string? AuthorLastName { get; set; }

            public string? AuthorProfileImageUrl
            { get; set; }

            public string PropertyTitle { get; set; } =
                string.Empty;

            public string? PropertyCountry { get; set; }

            public string? PropertyCity { get; set; }

            public string? PropertyCoverImageUrl
            { get; set; }

            public DateTimeOffset CreatedAt { get; set; }

            public DateTimeOffset? UpdatedAt { get; set; }

            public DateTimeOffset? ModeratedAt { get; set; }

            public DateTimeOffset? PublishedAt { get; set; }

            public DateTimeOffset? RejectedAt { get; set; }

            public Guid? ReplyId { get; set; }

            public Guid? ReplyHostProfileId { get; set; }

            public string? ReplyHostDisplayName { get; set; }

            public string? ReplyHostProfileImageUrl
            { get; set; }

            public string? ReplyContent { get; set; }

            public DateTimeOffset? ReplyCreatedAt
            { get; set; }

            public DateTimeOffset? ReplyUpdatedAt
            { get; set; }
        }
    }
}