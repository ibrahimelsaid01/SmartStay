using SmartStayDAL;

namespace SmartStayBLL
{
    public interface IHostPropertyManagementService
    {
        Task<HostPropertiesResponse> GetPropertiesAsync(
            Guid userId,
            int page,
            int pageSize,
            PropertyStatus? status,
            CancellationToken cancellationToken = default);

        Task<HostPropertyStatusSummaryResponse>
            GetSummaryAsync(
                Guid userId,
                CancellationToken cancellationToken = default);

        Task<HostPropertyUnpublishResponse>
            UnpublishAsync(
                Guid userId,
                Guid propertyId,
                CancellationToken cancellationToken = default);
    }
}