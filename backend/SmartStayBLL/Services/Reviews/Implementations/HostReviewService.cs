using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class HostReviewService
        : IHostReviewService
    {
        private const int MaximumPageSize = 100;

        private const int MaximumReplyLength = 2000;

        private readonly SmartStayDbContext _dbContext;

        public HostReviewService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext = dbContext;
        }

        public async Task<HostReviewsResponse>
            GetReviewsAsync(
                Guid hostUserId,
                Guid? propertyId,
                bool unansweredOnly,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                hostUserId,
                "host user");

            ValidatePagination(
                page,
                pageSize);

            if (propertyId.HasValue)
            {
                ValidateIdentifier(
                    propertyId.Value,
                    "property");
            }

            var hostProfile =
                await GetApprovedHostProfileAsync(
                    hostUserId,
                    cancellationToken);

            var query =
                BuildHostReviewsQuery(
                    hostProfile.Id);

            if (propertyId.HasValue)
            {
                var propertyBelongsToHost =
                    await _dbContext.Properties
                        .AsNoTracking()
                        .AnyAsync(
                            property =>
                                property.Id ==
                                    propertyId.Value
                                &&
                                property.HostProfileId ==
                                    hostProfile.Id,
                            cancellationToken);

                if (!propertyBelongsToHost)
                {
                    throw new KeyNotFoundException(
                        "The property was not found.");
                }

                query =
                    query.Where(review =>
                        review.PropertyId ==
                            propertyId.Value);
            }

            if (unansweredOnly)
            {
                query =
                    query.Where(review =>
                        review.ReplyId == null);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var items =
                await query
                    .OrderByDescending(review =>
                        review.PublishedAt)
                    .ThenByDescending(review =>
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

            return new HostReviewsResponse
            {
                Items =
                    items
                        .Select(MapResponse)
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

        public async Task<HostReviewResponse>
            GetByIdAsync(
                Guid hostUserId,
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                hostUserId,
                "host user");

            ValidateIdentifier(
                reviewId,
                "review");

            var hostProfile =
                await GetApprovedHostProfileAsync(
                    hostUserId,
                    cancellationToken);

            return await GetHostReviewResponseAsync(
                hostProfile.Id,
                reviewId,
                cancellationToken);
        }

        public async Task<HostReviewResponse>
            CreateReplyAsync(
                Guid hostUserId,
                Guid reviewId,
                UpsertReviewReplyRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                hostUserId,
                "host user");

            ValidateIdentifier(
                reviewId,
                "review");

            ArgumentNullException.ThrowIfNull(
                request);

            var content =
                NormalizeReplyContent(
                    request.Content);

            var hostProfile =
                await GetApprovedHostProfileAsync(
                    hostUserId,
                    cancellationToken);

            var review =
                await GetOwnedPublishedReviewAsync(
                    hostProfile.Id,
                    reviewId,
                    cancellationToken);

            var replyAlreadyExists =
                await _dbContext.ReviewReplies
                    .AnyAsync(
                        reply =>
                            reply.ReviewId ==
                                review.Id,
                        cancellationToken);

            if (replyAlreadyExists)
            {
                throw new InvalidOperationException(
                    "A reply already exists for this review.");
            }

            var reply =
                new ReviewReply
                {
                    Id =
                        Guid.NewGuid(),

                    ReviewId =
                        review.Id,

                    HostProfileId =
                        hostProfile.Id,

                    Content =
                        content,

                    CreatedAt =
                        DateTimeOffset.UtcNow
                };

            _dbContext.ReviewReplies.Add(
                reply);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return await GetHostReviewResponseAsync(
                hostProfile.Id,
                review.Id,
                cancellationToken);
        }

        public async Task<HostReviewResponse>
            UpdateReplyAsync(
                Guid hostUserId,
                Guid reviewId,
                UpsertReviewReplyRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                hostUserId,
                "host user");

            ValidateIdentifier(
                reviewId,
                "review");

            ArgumentNullException.ThrowIfNull(
                request);

            var content =
                NormalizeReplyContent(
                    request.Content);

            var hostProfile =
                await GetApprovedHostProfileAsync(
                    hostUserId,
                    cancellationToken);

            var reply =
                await _dbContext.ReviewReplies
                    .Include(item =>
                        item.Review)
                    .ThenInclude(review =>
                        review.Property)
                    .SingleOrDefaultAsync(
                        item =>
                            item.ReviewId ==
                                reviewId
                            &&
                            item.HostProfileId ==
                                hostProfile.Id
                            &&
                            item.Review.Status ==
                                ReviewStatus.Posted
                            &&
                            item.Review.Property
                                .HostProfileId ==
                                hostProfile.Id,
                        cancellationToken);

            if (reply is null)
            {
                throw new KeyNotFoundException(
                    "The review reply was not found.");
            }

            reply.Content =
                content;

            reply.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return await GetHostReviewResponseAsync(
                hostProfile.Id,
                reviewId,
                cancellationToken);
        }

        public async Task DeleteReplyAsync(
            Guid hostUserId,
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            ValidateIdentifier(
                hostUserId,
                "host user");

            ValidateIdentifier(
                reviewId,
                "review");

            var hostProfile =
                await GetApprovedHostProfileAsync(
                    hostUserId,
                    cancellationToken);

            var reply =
                await _dbContext.ReviewReplies
                    .Include(item =>
                        item.Review)
                    .ThenInclude(review =>
                        review.Property)
                    .SingleOrDefaultAsync(
                        item =>
                            item.ReviewId ==
                                reviewId
                            &&
                            item.HostProfileId ==
                                hostProfile.Id
                            &&
                            item.Review.Property
                                .HostProfileId ==
                                hostProfile.Id,
                        cancellationToken);

            if (reply is null)
            {
                throw new KeyNotFoundException(
                    "The review reply was not found.");
            }

            _dbContext.ReviewReplies.Remove(
                reply);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        private IQueryable<HostReviewProjection>
            BuildHostReviewsQuery(
                Guid hostProfileId)
        {
            return _dbContext.Reviews
                .AsNoTracking()
                .Where(review =>
                    review.Property.HostProfileId ==
                        hostProfileId
                    &&
                    review.Status ==
                        ReviewStatus.Posted)
                .Select(review =>
                    new HostReviewProjection
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

                        PublishedAt =
                            review.PublishedAt,

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

        private async Task<HostReviewResponse>
            GetHostReviewResponseAsync(
                Guid hostProfileId,
                Guid reviewId,
                CancellationToken cancellationToken)
        {
            var review =
                await BuildHostReviewsQuery(
                        hostProfileId)
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == reviewId,
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The published review was not found.");
            }

            return MapResponse(
                review);
        }

        private async Task<Review>
            GetOwnedPublishedReviewAsync(
                Guid hostProfileId,
                Guid reviewId,
                CancellationToken cancellationToken)
        {
            var review =
                await _dbContext.Reviews
                    .Include(item =>
                        item.Property)
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == reviewId
                            &&
                            item.Status ==
                                ReviewStatus.Posted
                            &&
                            item.Property.HostProfileId ==
                                hostProfileId,
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The published review was not found.");
            }

            return review;
        }

        private async Task<HostProfile>
            GetApprovedHostProfileAsync(
                Guid hostUserId,
                CancellationToken cancellationToken)
        {
            var hostProfile =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .Include(item =>
                        item.User)
                    .SingleOrDefaultAsync(
                        item =>
                            item.UserId ==
                                hostUserId,
                        cancellationToken);

            if (hostProfile is null)
            {
                throw new KeyNotFoundException(
                    "The host profile was not found.");
            }

            if (!hostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (hostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "The host account must be approved before managing review replies.");
            }

            return hostProfile;
        }

        private static HostReviewResponse MapResponse(
            HostReviewProjection review)
        {
            return new HostReviewResponse
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

                HelpfulCount =
                    review.HelpfulCount,

                Author =
                    new ReviewAuthorResponse
                    {
                        UserId =
                            review.AuthorUserId,

                        DisplayName =
                            BuildAuthorDisplayName(
                                review.AuthorFirstName,
                                review.AuthorLastName),

                        ProfileImageUrl =
                            review.AuthorProfileImageUrl
                    },

                Property =
                    new ReviewPropertyResponse
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
                    },

                Reply =
                    MapReply(
                        review),

                CreatedAt =
                    review.CreatedAt,

                PublishedAt =
                    review.PublishedAt
            };
        }

        private static ReviewReplyResponse? MapReply(
            HostReviewProjection review)
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

        private static string NormalizeReplyContent(
            string? content)
        {
            if (string.IsNullOrWhiteSpace(
                    content))
            {
                throw new ArgumentException(
                    "The reply content is required.");
            }

            var normalizedContent =
                content.Trim();

            if (normalizedContent.Length is < 2
                or > MaximumReplyLength)
            {
                throw new ArgumentException(
                    $"The reply must be between 2 and " +
                    $"{MaximumReplyLength} characters.");
            }

            return normalizedContent;
        }

        private static void ValidateIdentifier(
            Guid identifier,
            string identifierName)
        {
            if (identifier == Guid.Empty)
            {
                throw new ArgumentException(
                    $"The {identifierName} identifier is invalid.");
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

        private sealed class HostReviewProjection
        {
            public Guid Id { get; set; }

            public Guid BookingId { get; set; }

            public Guid PropertyId { get; set; }

            public int Rating { get; set; }

            public string? PositiveComment { get; set; }

            public string? NegativeComment { get; set; }

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

            public DateTimeOffset? PublishedAt { get; set; }

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