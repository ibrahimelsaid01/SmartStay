using SmartStayDAL;

namespace SmartStayBLL
{
    public interface IOtpService
    {
        Task<SendOtpResult> SendAsync(
            string email,
            OtpPurpose purpose,
            Guid? userId = null,
            CancellationToken cancellationToken = default);

        Task<OtpVerificationResult> VerifyAsync(
            string email,
            string code,
            OtpPurpose purpose,
            CancellationToken cancellationToken = default);
    }
}