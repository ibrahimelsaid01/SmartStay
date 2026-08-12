using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class HostReviewNotificationDecorator
        : IHostReviewService
    {
        private readonly HostReviewService
            _hostReviewService;

        private readonly SmartStayDbContext
            _dbContext;

        private readonly INotificationPublisher
            _notificationPublisher;

        public HostReviewNotificationDecorator(
            HostReviewService hostReviewService,
            SmartStayDbContext dbContext,
            INotificationPublisher notificationPublisher)
        {
            ArgumentNullException.ThrowIfNull(
                hostReviewService);

            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                notificationPublisher);

            _hostReviewService =
                hostReviewService;

            _dbContext =
                dbContext;

            _notificationPublisher =
                notificationPublisher;
        }

        public Task<HostReviewsResponse> GetReviewsAsync(
            Guid hostUserId,
            Guid? propertyId,
            bool unansweredOnly,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return _hostReviewService
                .GetReviewsAsync(
                    hostUserId,
                    propertyId,
                    unansweredOnly,
                    page,
                    pageSize,
                    cancellationToken);
        }

        public Task<HostReviewResponse> GetByIdAsync(
            Guid hostUserId,
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            return _hostReviewService
                .GetByIdAsync(
                    hostUserId,
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
            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                var response =
                    await _hostReviewService
                        .CreateReplyAsync(
                            hostUserId,
                            reviewId,
                            request,
                            cancellationToken);

                var recipient =
                    await GetReviewRecipientAsync(
                        reviewId,
                        cancellationToken);

                await _notificationPublisher
                    .PublishAsync(
                        new NotificationPublishRequest
                        {
                            UserId =
                                recipient.UserId,

                            Type =
                                NotificationType
                                    .ReviewReplyReceived,

                            Title =
                                "Host replied to your review",

                            Message =
                                $"The host of " +
                                $"\"{recipient.PropertyTitle}\" " +
                                $"replied to your review.",

                            ReferenceType =
                                NotificationReferenceType
                                    .Review,

                            ReferenceId =
                                reviewId,

                            DeduplicationKey =
                                NotificationDeduplicationKeys
                                    .ReviewReplyReceived(
                                        reviewId)
                        },
                        cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return response;
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }

        /*
         * Updating an existing reply does not send another
         * notification. This avoids repeated notifications
         * whenever the host corrects their response.
         */
        public Task<HostReviewResponse> UpdateReplyAsync(
            Guid hostUserId,
            Guid reviewId,
            UpsertReviewReplyRequest request,
            CancellationToken cancellationToken = default)
        {
            return _hostReviewService
                .UpdateReplyAsync(
                    hostUserId,
                    reviewId,
                    request,
                    cancellationToken);
        }

        /*
         * Deleting a reply also does not send a notification.
         */
        public Task DeleteReplyAsync(
            Guid hostUserId,
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            return _hostReviewService
                .DeleteReplyAsync(
                    hostUserId,
                    reviewId,
                    cancellationToken);
        }

        private async Task<ReviewRecipientProjection>
            GetReviewRecipientAsync(
                Guid reviewId,
                CancellationToken cancellationToken)
        {
            var recipient =
                await _dbContext.Reviews
                    .AsNoTracking()
                    .Where(review =>
                        review.Id == reviewId)
                    .Select(review =>
                        new ReviewRecipientProjection
                        {
                            UserId =
                                review.UserId,

                            PropertyTitle =
                                review.Property.Title
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (recipient is null)
            {
                throw new KeyNotFoundException(
                    "The review notification recipient was not found.");
            }

            return recipient;
        }

        private sealed class ReviewRecipientProjection
        {
            public Guid UserId { get; set; }

            public string PropertyTitle { get; set; } =
                string.Empty;
        }
    }
}