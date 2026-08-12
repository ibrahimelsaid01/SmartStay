namespace SmartStayBLL
{
    public interface IAdminHostApplicationService
    {
        Task<IReadOnlyList<
            AdminHostApplicationSummaryResponse>>
            GetPendingAsync(
                CancellationToken cancellationToken = default);

        Task<AdminHostApplicationDetailsResponse>
            GetByIdAsync(
                Guid applicationId,
                CancellationToken cancellationToken = default);

        Task<ImageContentResult>
            GetIdentityDocumentImageAsync(
                Guid applicationId,
                HostIdentityDocumentSide side,
                CancellationToken cancellationToken = default);

        Task<AdminHostApplicationDetailsResponse>
            ApproveAsync(
                Guid applicationId,
                CancellationToken cancellationToken = default);

        Task<AdminHostApplicationDetailsResponse>
    RejectAsync(
        Guid applicationId,
        RejectHostApplicationRequest request,
        CancellationToken cancellationToken = default);
    }
}