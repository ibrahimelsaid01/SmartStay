using SmartStayDAL;

namespace SmartStayBLL
{
    public interface IReviewService
    {
        /*
         * Guest operations.
         */

        Task<UserReviewResponse> CreateAsync(
            Guid userId,
            Guid bookingId,
            CreateReviewRequest request,
            CancellationToken cancellationToken = default);

        Task<MyReviewsResponse> GetMyReviewsAsync(
            Guid userId,
            ReviewStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<UserReviewResponse> GetMyReviewByIdAsync(
            Guid userId,
            Guid reviewId,
            CancellationToken cancellationToken = default);

        Task<UserReviewResponse> UpdateAsync(
            Guid userId,
            Guid reviewId,
            UpdateReviewRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid userId,
            Guid reviewId,
            CancellationToken cancellationToken = default);

        /*
         * Public property review operations.
         */

        Task<PropertyReviewsResponse> GetPropertyReviewsAsync(
            Guid propertyId,
            Guid? currentUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<PropertyRatingSummaryResponse>
            GetPropertyRatingSummaryAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FeaturedReviewResponse>>
            GetFeaturedReviewsAsync(
                int limit,
                CancellationToken cancellationToken = default);

        /*
         * Helpful voting.
         */

        Task<ReviewHelpfulResponse> MarkHelpfulAsync(
            Guid userId,
            Guid reviewId,
            CancellationToken cancellationToken = default);

        Task<ReviewHelpfulResponse> RemoveHelpfulAsync(
            Guid userId,
            Guid reviewId,
            CancellationToken cancellationToken = default);
    }
}