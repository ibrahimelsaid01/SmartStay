using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class ReviewService
        : IReviewService
    {
        private const int MaximumPageSize = 100;
        private const int MaximumFeaturedReviews = 3;

        private const int MaximumCommentLength = 2000;

        private readonly SmartStayDbContext _dbContext;

        public ReviewService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext = dbContext;
        }

        // =====================================================
        // Create review
        // =====================================================

        public async Task<UserReviewResponse> CreateAsync(
            Guid userId,
            Guid bookingId,
            CreateReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateReviewId(bookingId, "booking");

            ArgumentNullException.ThrowIfNull(request);

            var comments =
                ValidateAndNormalizeReviewContent(
                    request.Rating,
                    request.PositiveComment,
                    request.NegativeComment);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            /*
             * A guest can review only their own
             * completed booking.
             */
            var booking =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == bookingId
                        &&
                        item.GuestUserId == userId)
                    .Select(item =>
                        new
                        {
                            item.Id,
                            item.PropertyId,
                            item.Status
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    "The booking was not found.");
            }

            if (booking.Status != BookingStatus.Completed)
            {
                throw new InvalidOperationException(
                    "A review can be created only after the booking has been completed.");
            }

            var reviewAlreadyExists =
                await _dbContext.Reviews
                    .AnyAsync(
                        review =>
                            review.BookingId == bookingId,
                        cancellationToken);

            if (reviewAlreadyExists)
            {
                throw new InvalidOperationException(
                    "A review already exists for this booking.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            var review =
                new Review
                {
                    Id =
                        Guid.NewGuid(),

                    BookingId =
                        booking.Id,

                    PropertyId =
                        booking.PropertyId,

                    UserId =
                        userId,

                    Rating =
                        request.Rating,

                    PositiveComment =
                        comments.PositiveComment,

                    NegativeComment =
                        comments.NegativeComment,

                    Status =
                        ReviewStatus.Pending,

                    RejectionReason =
                        null,

                    ModeratedByUserId =
                        null,

                    CreatedAt =
                        currentTime,

                    UpdatedAt =
                        null,

                    ModeratedAt =
                        null,

                    PublishedAt =
                        null,

                    RejectedAt =
                        null
                };

            _dbContext.Reviews.Add(
                review);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return await GetOwnedReviewResponseAsync(
                userId,
                review.Id,
                cancellationToken);
        }

        // =====================================================
        // My reviews
        // =====================================================

        public async Task<MyReviewsResponse>
            GetMyReviewsAsync(
                Guid userId,
                ReviewStatus? status,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidatePagination(page, pageSize);
            ValidateOptionalReviewStatus(status);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var query =
                BuildOwnedReviewsQuery(userId);

            if (status.HasValue)
            {
                query =
                    query.Where(review =>
                        review.Status == status.Value);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var projectedItems =
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

            return new MyReviewsResponse
            {
                Items =
                    projectedItems
                        .Select(MapUserReviewResponse)
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

        // =====================================================
        // Get my review by id
        // =====================================================

        public async Task<UserReviewResponse>
            GetMyReviewByIdAsync(
                Guid userId,
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateReviewId(reviewId, "review");

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            return await GetOwnedReviewResponseAsync(
                userId,
                reviewId,
                cancellationToken);
        }

        // =====================================================
        // Update / resubmit review
        // =====================================================

        public async Task<UserReviewResponse> UpdateAsync(
            Guid userId,
            Guid reviewId,
            UpdateReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateReviewId(reviewId, "review");

            ArgumentNullException.ThrowIfNull(request);

            var comments =
                ValidateAndNormalizeReviewContent(
                    request.Rating,
                    request.PositiveComment,
                    request.NegativeComment);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var review =
                await _dbContext.Reviews
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == reviewId
                            &&
                            item.UserId == userId,
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The review was not found.");
            }

            /*
             * Published reviews cannot be edited directly.
             *
             * Pending reviews can be updated.
             * Rejected reviews can be corrected and resubmitted.
             */
            if (review.Status == ReviewStatus.Posted)
            {
                throw new InvalidOperationException(
                    "A published review cannot be edited.");
            }

            review.Rating =
                request.Rating;

            review.PositiveComment =
                comments.PositiveComment;

            review.NegativeComment =
                comments.NegativeComment;

            /*
             * Editing a rejected review sends it back
             * to the moderation queue.
             */
            review.Status =
                ReviewStatus.Pending;

            review.RejectionReason =
                null;

            review.ModeratedByUserId =
                null;

            review.ModeratedAt =
                null;

            review.PublishedAt =
                null;

            review.RejectedAt =
                null;

            review.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return await GetOwnedReviewResponseAsync(
                userId,
                reviewId,
                cancellationToken);
        }

        // =====================================================
        // Delete review
        // =====================================================

        public async Task DeleteAsync(
            Guid userId,
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateReviewId(reviewId, "review");

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var review =
                await _dbContext.Reviews
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == reviewId
                            &&
                            item.UserId == userId,
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The review was not found.");
            }

            /*
             * ReviewReply and ReviewHelpfulVotes are
             * deleted automatically by Cascade Delete.
             */
            _dbContext.Reviews.Remove(
                review);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        // =====================================================
        // Public property reviews
        // =====================================================

        public async Task<PropertyReviewsResponse>
            GetPropertyReviewsAsync(
                Guid propertyId,
                Guid? currentUserId,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            ValidatePropertyId(propertyId);
            ValidatePagination(page, pageSize);

            await EnsurePublicPropertyExistsAsync(
                propertyId,
                cancellationToken);

            var reviewsQuery =
                _dbContext.Reviews
                    .AsNoTracking()
                    .Where(review =>
                        review.PropertyId == propertyId
                        &&
                        review.Status ==
                            ReviewStatus.Posted);

            var totalCount =
                await reviewsQuery.CountAsync(
                    cancellationToken);

            var hasCurrentUser =
                currentUserId.HasValue
                &&
                currentUserId.Value != Guid.Empty;

            var currentUserIdentifier =
                currentUserId
                ??
                Guid.Empty;

            var projectedItems =
                await reviewsQuery
                    .OrderByDescending(review =>
                        review.PublishedAt)
                    .ThenByDescending(review =>
                        review.CreatedAt)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(review =>
                        new PublicReviewProjection
                        {
                            Id =
                                review.Id,

                            Rating =
                                review.Rating,

                            PositiveComment =
                                review.PositiveComment,

                            NegativeComment =
                                review.NegativeComment,

                            HelpfulCount =
                                review.HelpfulVotes.Count,

                            IsHelpfulByCurrentUser =
                                hasCurrentUser
                                &&
                                review.HelpfulVotes.Any(
                                    vote =>
                                        vote.UserId ==
                                            currentUserIdentifier),

                            UserId =
                                review.UserId,

                            AuthorFirstName =
                                review.User.FirstName,

                            AuthorLastName =
                                review.User.LastName,

                            AuthorProfileImageUrl =
                                review.User.ProfileImageUrl,

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
                                    : review.Reply
                                        .HostProfile
                                        .DisplayName,

                            ReplyHostProfileImageUrl =
                                review.Reply == null
                                    ? null
                                    : review.Reply
                                        .HostProfile
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
                        })
                    .ToListAsync(
                        cancellationToken);

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount
                        /
                        (double)pageSize);

            return new PropertyReviewsResponse
            {
                PropertyId =
                    propertyId,

                Items =
                    projectedItems
                        .Select(MapPublicReviewResponse)
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

        // =====================================================
        // Property rating summary
        // =====================================================

        public async Task<PropertyRatingSummaryResponse>
            GetPropertyRatingSummaryAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            ValidatePropertyId(propertyId);

            await EnsurePublicPropertyExistsAsync(
                propertyId,
                cancellationToken);

            var ratingGroups =
                await _dbContext.Reviews
                    .AsNoTracking()
                    .Where(review =>
                        review.PropertyId == propertyId
                        &&
                        review.Status ==
                            ReviewStatus.Posted)
                    .GroupBy(review =>
                        review.Rating)
                    .Select(group =>
                        new RatingGroupProjection
                        {
                            Rating =
                                group.Key,

                            Count =
                                group.Count()
                        })
                    .ToListAsync(
                        cancellationToken);

            var distribution =
                new Dictionary<int, int>
                {
                    [1] = 0,
                    [2] = 0,
                    [3] = 0,
                    [4] = 0,
                    [5] = 0
                };

            foreach (var group in ratingGroups)
            {
                distribution[group.Rating] =
                    group.Count;
            }

            var reviewsCount =
                ratingGroups.Sum(group =>
                    group.Count);

            var ratingTotal =
                ratingGroups.Sum(group =>
                    group.Rating
                    *
                    group.Count);

            var averageRating =
                reviewsCount == 0
                    ? 0
                    : Math.Round(
                        ratingTotal
                        /
                        (decimal)reviewsCount,
                        2,
                        MidpointRounding
                            .AwayFromZero);

            return new PropertyRatingSummaryResponse
            {
                PropertyId =
                    propertyId,

                AverageRating =
                    averageRating,

                ReviewsCount =
                    reviewsCount,

                Distribution =
                    distribution
            };
        }
        // =====================================================
        // Featured reviews for Home
        // =====================================================

        public async Task<IReadOnlyList<FeaturedReviewResponse>>
            GetFeaturedReviewsAsync(
                int limit,
                CancellationToken cancellationToken = default)
        {
            ValidateFeaturedReviewsLimit(limit);

            /*
             * Only publicly eligible reviews participate:
             *
             * - The review is Posted.
             * - The guest account is active.
             * - The property is Published.
             * - The host application is approved.
             * - The host account is active.
             */
            var eligibleReviews =
                _dbContext.Reviews
                    .AsNoTracking()
                    .Where(review =>
                        review.Status ==
                            ReviewStatus.Posted
                        &&
                        review.User.IsActive
                        &&
                        review.Property.Status ==
                            PropertyStatus.Published
                        &&
                        review.Property.HostProfile.Status ==
                            HostApplicationStatus.Approved
                        &&
                        review.Property.HostProfile.User
                            .IsActive);

            /*
             * GroupBy + First keeps only the newest eligible
             * review for each guest. This prevents one user
             * from occupying more than one Home review card.
             */
            var projectedItems =
                await eligibleReviews
                    .GroupBy(review =>
                        review.UserId)
                    .Select(group =>
                        group
                            .OrderByDescending(review =>
                                review.PublishedAt
                                ??
                                review.CreatedAt)
                            .ThenByDescending(review =>
                                review.CreatedAt)
                            .Select(review =>
                                new FeaturedReviewProjection
                                {
                                    Id =
                                        review.Id,

                                    Rating =
                                        review.Rating,

                                    PositiveComment =
                                        review.PositiveComment,

                                    NegativeComment =
                                        review.NegativeComment,

                                    UserId =
                                        review.UserId,

                                    AuthorFirstName =
                                        review.User.FirstName,

                                    AuthorLastName =
                                        review.User.LastName,

                                    AuthorProfileImageUrl =
                                        review.User.ProfileImageUrl,

                                    PropertyId =
                                        review.PropertyId,

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

                                    PublishedAt =
                                        review.PublishedAt
                                        ??
                                        review.CreatedAt
                                })
                            .First())
                    .OrderByDescending(review =>
                        review.PublishedAt)
                    .Take(limit)
                    .ToListAsync(
                        cancellationToken);

            return projectedItems
                .Select(review =>
                    new FeaturedReviewResponse
                    {
                        Id =
                            review.Id,

                        Rating =
                            review.Rating,

                        /*
                         * Prefer the positive comment for the
                         * Home testimonial card. If it does not
                         * exist, use the negative comment.
                         */
                        Comment =
                            review.PositiveComment
                            ??
                            review.NegativeComment
                            ??
                            string.Empty,

                        Author =
                            new ReviewAuthorResponse
                            {
                                UserId =
                                    review.UserId,

                                DisplayName =
                                    BuildPublicAuthorDisplayName(
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
                                    ??
                                    string.Empty,

                                City =
                                    review.PropertyCity
                                    ??
                                    string.Empty,

                                CoverImageUrl =
                                    review.PropertyCoverImageUrl
                            },

                        PublishedAt =
                            review.PublishedAt
                    })
                .ToList();
        }
        // =====================================================
        // Helpful vote
        // =====================================================

        public async Task<ReviewHelpfulResponse>
            MarkHelpfulAsync(
                Guid userId,
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateReviewId(reviewId, "review");

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var review =
                await _dbContext.Reviews
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == reviewId)
                    .Select(item =>
                        new
                        {
                            item.Id,
                            item.UserId,
                            item.Status
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The review was not found.");
            }

            if (review.Status != ReviewStatus.Posted)
            {
                throw new InvalidOperationException(
                    "Only published reviews can be marked as helpful.");
            }

            if (review.UserId == userId)
            {
                throw new InvalidOperationException(
                    "You cannot mark your own review as helpful.");
            }

            var voteExists =
                await _dbContext.ReviewHelpfulVotes
                    .AnyAsync(
                        vote =>
                            vote.ReviewId == reviewId
                            &&
                            vote.UserId == userId,
                        cancellationToken);

            /*
             * Idempotent behavior:
             *
             * Repeating POST does not create duplicate votes.
             */
            if (!voteExists)
            {
                var vote =
                    new ReviewHelpfulVote
                    {
                        ReviewId =
                            reviewId,

                        UserId =
                            userId,

                        CreatedAt =
                            DateTimeOffset.UtcNow
                    };

                _dbContext.ReviewHelpfulVotes.Add(
                    vote);

                try
                {
                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }
                catch (DbUpdateException)
                {
                    /*
                     * Handles two simultaneous requests.
                     *
                     * If the vote now exists, the request is
                     * treated as successful.
                     */
                    _dbContext.Entry(vote).State =
                        EntityState.Detached;

                    var voteWasCreated =
                        await _dbContext
                            .ReviewHelpfulVotes
                            .AsNoTracking()
                            .AnyAsync(
                                item =>
                                    item.ReviewId ==
                                        reviewId
                                    &&
                                    item.UserId ==
                                        userId,
                                cancellationToken);

                    if (!voteWasCreated)
                    {
                        throw;
                    }
                }
            }

            return await BuildHelpfulResponseAsync(
                reviewId,
                userId,
                cancellationToken);
        }

        public async Task<ReviewHelpfulResponse>
            RemoveHelpfulAsync(
                Guid userId,
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateReviewId(reviewId, "review");

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var reviewExists =
                await _dbContext.Reviews
                    .AsNoTracking()
                    .AnyAsync(
                        review =>
                            review.Id == reviewId
                            &&
                            review.Status ==
                                ReviewStatus.Posted,
                        cancellationToken);

            if (!reviewExists)
            {
                throw new KeyNotFoundException(
                    "The published review was not found.");
            }

            var vote =
                await _dbContext.ReviewHelpfulVotes
                    .SingleOrDefaultAsync(
                        item =>
                            item.ReviewId == reviewId
                            &&
                            item.UserId == userId,
                        cancellationToken);

            /*
             * Idempotent behavior:
             *
             * Repeating DELETE remains successful.
             */
            if (vote is not null)
            {
                _dbContext.ReviewHelpfulVotes.Remove(
                    vote);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            return await BuildHelpfulResponseAsync(
                reviewId,
                userId,
                cancellationToken);
        }

        // =====================================================
        // Queries and mapping
        // =====================================================

        private IQueryable<UserReviewProjection>
            BuildOwnedReviewsQuery(
                Guid userId)
        {
            return _dbContext.Reviews
                .AsNoTracking()
                .Where(review =>
                    review.UserId == userId)
                .Select(review =>
                    new UserReviewProjection
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
                            review.Status,

                        RejectionReason =
                            review.RejectionReason,

                        CheckInDate =
                            review.Booking.CheckInDate,

                        CheckOutDate =
                            review.Booking.CheckOutDate,

                        HelpfulCount =
                            review.HelpfulVotes.Count,

                        PropertyId =
                            review.PropertyId,

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
                                : review.Reply
                                    .HostProfile
                                    .DisplayName,

                        ReplyHostProfileImageUrl =
                            review.Reply == null
                                ? null
                                : review.Reply
                                    .HostProfile
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

        private async Task<UserReviewResponse>
            GetOwnedReviewResponseAsync(
                Guid userId,
                Guid reviewId,
                CancellationToken cancellationToken)
        {
            var review =
                await BuildOwnedReviewsQuery(userId)
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == reviewId,
                        cancellationToken);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "The review was not found.");
            }

            return MapUserReviewResponse(
                review);
        }

        private static UserReviewResponse
            MapUserReviewResponse(
                UserReviewProjection review)
        {
            return new UserReviewResponse
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

                CanEdit =
                    review.Status ==
                        ReviewStatus.Pending
                    ||
                    review.Status ==
                        ReviewStatus.Rejected,

                CanDelete =
                    true,

                Property =
                    new ReviewPropertyResponse
                    {
                        Id =
                            review.PropertyId,

                        Title =
                            review.PropertyTitle,

                        Country =
                            review.PropertyCountry
                            ??
                            string.Empty,

                        City =
                            review.PropertyCity
                            ??
                            string.Empty,

                        CoverImageUrl =
                            review
                                .PropertyCoverImageUrl
                    },

                Reply =
                    MapReply(
                        review.ReplyId,
                        review.ReplyHostProfileId,
                        review.ReplyHostDisplayName,
                        review.ReplyHostProfileImageUrl,
                        review.ReplyContent,
                        review.ReplyCreatedAt,
                        review.ReplyUpdatedAt),

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

        private static PublicReviewResponse
            MapPublicReviewResponse(
                PublicReviewProjection review)
        {
            return new PublicReviewResponse
            {
                Id =
                    review.Id,

                Rating =
                    review.Rating,

                PositiveComment =
                    review.PositiveComment,

                NegativeComment =
                    review.NegativeComment,

                HelpfulCount =
                    review.HelpfulCount,

                IsHelpfulByCurrentUser =
                    review.IsHelpfulByCurrentUser,

                Author =
                    new ReviewAuthorResponse
                    {
                        UserId =
                            review.UserId,

                        DisplayName =
                            BuildPublicAuthorDisplayName(
                                review.AuthorFirstName,
                                review.AuthorLastName),

                        ProfileImageUrl =
                            review
                                .AuthorProfileImageUrl
                    },

                Reply =
                    MapReply(
                        review.ReplyId,
                        review.ReplyHostProfileId,
                        review.ReplyHostDisplayName,
                        review.ReplyHostProfileImageUrl,
                        review.ReplyContent,
                        review.ReplyCreatedAt,
                        review.ReplyUpdatedAt),

                CreatedAt =
                    review.CreatedAt,

                PublishedAt =
                    review.PublishedAt
            };
        }

        private static ReviewReplyResponse? MapReply(
            Guid? replyId,
            Guid? hostProfileId,
            string? hostDisplayName,
            string? hostProfileImageUrl,
            string? content,
            DateTimeOffset? createdAt,
            DateTimeOffset? updatedAt)
        {
            if (!replyId.HasValue
                ||
                !hostProfileId.HasValue
                ||
                string.IsNullOrWhiteSpace(content)
                ||
                !createdAt.HasValue)
            {
                return null;
            }

            return new ReviewReplyResponse
            {
                Id =
                    replyId.Value,

                HostProfileId =
                    hostProfileId.Value,

                HostDisplayName =
                    hostDisplayName
                    ??
                    string.Empty,

                HostProfileImageUrl =
                    hostProfileImageUrl,

                Content =
                    content,

                CreatedAt =
                    createdAt.Value,

                UpdatedAt =
                    updatedAt
            };
        }

        private async Task<ReviewHelpfulResponse>
            BuildHelpfulResponseAsync(
                Guid reviewId,
                Guid userId,
                CancellationToken cancellationToken)
        {
            var response =
                await _dbContext.Reviews
                    .AsNoTracking()
                    .Where(review =>
                        review.Id == reviewId
                        &&
                        review.Status ==
                            ReviewStatus.Posted)
                    .Select(review =>
                        new ReviewHelpfulResponse
                        {
                            ReviewId =
                                review.Id,

                            HelpfulCount =
                                review.HelpfulVotes.Count,

                            IsHelpfulByCurrentUser =
                                review.HelpfulVotes.Any(
                                    vote =>
                                        vote.UserId ==
                                            userId)
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (response is null)
            {
                throw new KeyNotFoundException(
                    "The published review was not found.");
            }

            return response;
        }

        // =====================================================
        // Database validation
        // =====================================================

        private async Task EnsureActiveUserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var user =
                await _dbContext.Users
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == userId)
                    .Select(item =>
                        new
                        {
                            item.IsActive
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "The user was not found.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }
        }

        private async Task EnsurePublicPropertyExistsAsync(
            Guid propertyId,
            CancellationToken cancellationToken)
        {
            var propertyExists =
                await _dbContext.Properties
                    .AsNoTracking()
                    .AnyAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.Status ==
                                PropertyStatus.Published
                            &&
                            property.HostProfile.Status ==
                                HostApplicationStatus.Approved
                            &&
                            property.HostProfile.User
                                .IsActive,
                        cancellationToken);

            if (!propertyExists)
            {
                throw new KeyNotFoundException(
                    "The published property was not found.");
            }
        }

        // =====================================================
        // Request validation
        // =====================================================

        private static NormalizedReviewComments
            ValidateAndNormalizeReviewContent(
                int rating,
                string? positiveComment,
                string? negativeComment)
        {
            if (rating is < 1 or > 5)
            {
                throw new ArgumentException(
                    "Rating must be between 1 and 5.");
            }

            var normalizedPositiveComment =
                NormalizeOptionalComment(
                    positiveComment);

            var normalizedNegativeComment =
                NormalizeOptionalComment(
                    negativeComment);

            if (normalizedPositiveComment is null
                &&
                normalizedNegativeComment is null)
            {
                throw new ArgumentException(
                    "At least one positive or negative comment is required.");
            }

            return new NormalizedReviewComments(
                normalizedPositiveComment,
                normalizedNegativeComment);
        }

        private static string? NormalizeOptionalComment(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalizedValue =
                value.Trim();

            if (normalizedValue.Length >
                MaximumCommentLength)
            {
                throw new ArgumentException(
                    $"Review comments cannot exceed " +
                    $"{MaximumCommentLength} characters.");
            }

            return normalizedValue;
        }

        private static string
            BuildPublicAuthorDisplayName(
                string? firstName,
                string? lastName)
        {
            var normalizedFirstName =
                string.IsNullOrWhiteSpace(firstName)
                    ? "SmartStay guest"
                    : firstName.Trim();

            if (string.IsNullOrWhiteSpace(lastName))
            {
                return normalizedFirstName;
            }

            var lastNameInitial =
                char.ToUpperInvariant(
                    lastName.Trim()[0]);

            return $"{normalizedFirstName} " +
                   $"{lastNameInitial}.";
        }

        private static void ValidateUserId(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The user identifier is invalid.");
            }
        }

        private static void ValidatePropertyId(
            Guid propertyId)
        {
            ValidateReviewId(
                propertyId,
                "property");
        }

        private static void ValidateReviewId(
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
        private static void ValidateFeaturedReviewsLimit(
    int limit)
        {
            if (limit < 1
                ||
                limit > MaximumFeaturedReviews)
            {
                throw new ArgumentException(
                    $"Featured reviews limit must be between 1 and " +
                    $"{MaximumFeaturedReviews}.");
            }
        }
        private static void ValidateOptionalReviewStatus(
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

        // =====================================================
        // Internal records and projections
        // =====================================================

        private sealed record NormalizedReviewComments(
            string? PositiveComment,
            string? NegativeComment);

        private sealed class RatingGroupProjection
        {
            public int Rating { get; set; }

            public int Count { get; set; }
        }

        private sealed class UserReviewProjection
        {
            public Guid Id { get; set; }

            public Guid BookingId { get; set; }

            public int Rating { get; set; }

            public string? PositiveComment { get; set; }

            public string? NegativeComment { get; set; }

            public ReviewStatus Status { get; set; }

            public string? RejectionReason { get; set; }

            public DateOnly CheckInDate { get; set; }

            public DateOnly CheckOutDate { get; set; }

            public int HelpfulCount { get; set; }

            public Guid PropertyId { get; set; }

            public string PropertyTitle { get; set; } =
                string.Empty;

            public string? PropertyCountry { get; set; }

            public string? PropertyCity { get; set; }

            public string? PropertyCoverImageUrl { get; set; }

            public DateTimeOffset CreatedAt { get; set; }

            public DateTimeOffset? UpdatedAt { get; set; }

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
        private sealed class FeaturedReviewProjection
        {
            public Guid Id { get; set; }

            public int Rating { get; set; }

            public string? PositiveComment { get; set; }

            public string? NegativeComment { get; set; }

            public Guid UserId { get; set; }

            public string? AuthorFirstName { get; set; }

            public string? AuthorLastName { get; set; }

            public string? AuthorProfileImageUrl
            { get; set; }

            public Guid PropertyId { get; set; }

            public string PropertyTitle { get; set; } =
                string.Empty;

            public string? PropertyCountry { get; set; }

            public string? PropertyCity { get; set; }

            public string? PropertyCoverImageUrl
            { get; set; }

            public DateTimeOffset PublishedAt { get; set; }
        }
        private sealed class PublicReviewProjection
        {
            public Guid Id { get; set; }

            public int Rating { get; set; }

            public string? PositiveComment { get; set; }

            public string? NegativeComment { get; set; }

            public int HelpfulCount { get; set; }

            public bool IsHelpfulByCurrentUser { get; set; }

            public Guid UserId { get; set; }

            public string? AuthorFirstName { get; set; }

            public string? AuthorLastName { get; set; }

            public string? AuthorProfileImageUrl
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