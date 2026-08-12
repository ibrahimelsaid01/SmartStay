namespace SmartStayBLL
{
    public interface IAdminPropertyService
    {
        Task<AdminPendingPropertiesResponse>
            GetPendingAsync(
                int page,
                int pageSize,
                CancellationToken cancellationToken = default);

        Task<AdminPropertyDetailsResponse>
            GetByIdAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default);

        Task<ImageContentResult>
            GetVerificationDocumentPageContentAsync(
                Guid propertyId,
                Guid pageId,
                CancellationToken cancellationToken = default);

        Task<AdminPropertyReviewResponse>
            ApproveAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default);

        Task<AdminPropertyReviewResponse>
            RejectAsync(
                Guid propertyId,
                string reason,
                CancellationToken cancellationToken = default);
    }
}