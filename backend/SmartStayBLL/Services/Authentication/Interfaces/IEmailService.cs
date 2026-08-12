namespace SmartStayBLL
{
    public interface IEmailService
    {
        Task SendOtpAsync(
            string email,
            string code,
            CancellationToken cancellationToken = default);
    }
}