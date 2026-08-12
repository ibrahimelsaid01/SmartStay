namespace SmartStayBLL
{
    public interface IAccountService
    {
        Task<AccountDeactivationResponse> DeactivateAsync(
            Guid userId,
            DeactivateAccountRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default);
    }
}