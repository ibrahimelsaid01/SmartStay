namespace SmartStayBLL
{
    public interface IExternalAuthService
    {
        Task<ExternalUserInfo> ValidateAsync(
            string provider,
            string token,
            CancellationToken cancellationToken = default);
    }
}