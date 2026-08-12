using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminReviewNotificationDecorator
        : IAdminReviewService
    {
        private readonly AdminReviewService
            _adminReviewService;

        private readonly SmartStayDbContext
            _dbContext;

        private readonly INotificationPublisher
            _notificationPublisher;

        public AdminReviewNotificationDecorator(
            AdminReviewService adminReviewService,
            SmartStayDbContext dbContext,
            INotificationPublisher notificationPublisher)
        {
            ArgumentNullException.ThrowIfNull(
                adminReviewService);

            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                notificationPublisher);

            _adminReviewService =
                adminReviewService;

            _dbContext =
                dbContext;

            _notificationPublisher =
                notificationPublisher;
        }

        public Task<AdminReviewsResponse> GetReviewsAsync(
            ReviewStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return _adminReviewService
                .GetReviewsAsync(
                    status,
                    page,
                    pageSize,
                    cancellationToken);
        }

        public Task<AdminReviewDetailsResponse> GetByIdAsync(
            Guid reviewId,
            CancellationToken cancellationToken = default)
        {
            return _adminReviewService
                .GetByIdAsync(
                    reviewId,
                    cancellationToken);
        }

        public async Task<AdminReviewModerationResponse>
            ApproveAsync(
                Guid adminUserId,
                Guid reviewId,
                CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                var response =
                    await _adminReviewService
                        .ApproveAsync(
                            adminUserId,
                            reviewId,
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
                                    .ReviewApproved,

                            Title =
                                "Review approved",

                            Message =
                                $"Your review for " +
                                $"\"{recipient.PropertyTitle}\" " +
                                $"was approved and is now visible.",

                            ReferenceType =
                                NotificationReferenceType
                                    .Review,

                            ReferenceId =
                                reviewId,

                            DeduplicationKey =
                                NotificationDeduplicationKeys
                                    .ReviewApproved(
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

        public async Task<AdminReviewModerationResponse>
            RejectAsync(
                Guid adminUserId,
                Guid reviewId,
                RejectReviewRequest request,
                CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                var response =
                    await _adminReviewService
                        .RejectAsync(
                            adminUserId,
                            reviewId,
                            request,
                            cancellationToken);

                var recipient =
                    await GetReviewRecipientAsync(
                        reviewId,
                        cancellationToken);

                var reason =
                    response.RejectionReason
                    ??
                    "Please review your submitted content.";

                await _notificationPublisher
                    .PublishAsync(
                        new NotificationPublishRequest
                        {
                            UserId =
                                recipient.UserId,

                            Type =
                                NotificationType
                                    .ReviewRejected,

                            Title =
                                "Review needs changes",

                            Message =
                                $"Your review for " +
                                $"\"{recipient.PropertyTitle}\" " +
                                $"was rejected. Reason: {reason}",

                            ReferenceType =
                                NotificationReferenceType
                                    .Review,

                            ReferenceId =
                                reviewId,

                            DeduplicationKey =
                                NotificationDeduplicationKeys
                                    .ReviewRejected(
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