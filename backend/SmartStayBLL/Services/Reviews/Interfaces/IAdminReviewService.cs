using SmartStayDAL;

namespace SmartStayBLL
{
    public interface IAdminReviewService
    {
        Task<AdminReviewsResponse> GetReviewsAsync(
            ReviewStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<AdminReviewDetailsResponse> GetByIdAsync(
            Guid reviewId,
            CancellationToken cancellationToken = default);

        Task<AdminReviewModerationResponse> ApproveAsync(
            Guid adminUserId,
            Guid reviewId,
            CancellationToken cancellationToken = default);

        Task<AdminReviewModerationResponse> RejectAsync(
            Guid adminUserId,
            Guid reviewId,
            RejectReviewRequest request,
            CancellationToken cancellationToken = default);
    }
}