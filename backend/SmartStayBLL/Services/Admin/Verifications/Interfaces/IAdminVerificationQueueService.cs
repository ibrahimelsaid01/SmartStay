namespace SmartStayBLL
{
    public interface IAdminVerificationQueueService
    {
        Task<AdminVerificationQueueResponse> GetQueueAsync(
            AdminVerificationQueueRequest request,
            CancellationToken cancellationToken = default);

        Task<AdminVerificationHistoryResponse> GetHistoryAsync(
            string verificationType,
            Guid verificationId,
            CancellationToken cancellationToken = default);
    }
}