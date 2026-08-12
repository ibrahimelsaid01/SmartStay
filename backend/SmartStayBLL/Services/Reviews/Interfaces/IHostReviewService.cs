namespace SmartStayBLL
{
    public interface IHostReviewService
    {
        Task<HostReviewsResponse> GetReviewsAsync(
            Guid hostUserId,
            Guid? propertyId,
            bool unansweredOnly,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<HostReviewResponse> GetByIdAsync(
            Guid hostUserId,
            Guid reviewId,
            CancellationToken cancellationToken = default);

        Task<HostReviewResponse> CreateReplyAsync(
            Guid hostUserId,
            Guid reviewId,
            UpsertReviewReplyRequest request,
            CancellationToken cancellationToken = default);

        Task<HostReviewResponse> UpdateReplyAsync(
            Guid hostUserId,
            Guid reviewId,
            UpsertReviewReplyRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteReplyAsync(
            Guid hostUserId,
            Guid reviewId,
            CancellationToken cancellationToken = default);
    }
}