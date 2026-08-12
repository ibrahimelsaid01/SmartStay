namespace SmartStayBLL
{
    public interface IAuthService
    {
        Task<SendOtpResult> SendOtpAsync(
            SendOtpRequest request,
            CancellationToken cancellationToken = default);

        Task<AuthResult> VerifyOtpAsync(
            VerifyOtpRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthResult> ExternalLoginAsync(
            ExternalLoginRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthenticatedUserResponse> CompleteProfileAsync(
            Guid userId,
            CompleteProfileRequest request,
            CancellationToken cancellationToken = default);

        Task<AuthResult> RefreshAsync(
            string refreshToken,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        /*
         * Revokes only the refresh token stored
         * in the current device cookie.
         */
        Task LogoutAsync(
            string refreshToken,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        /*
         * Revokes every active refresh token that belongs
         * to the authenticated user.
         */
        Task LogoutFromAllDevicesAsync(
            Guid userId,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<AuthenticatedUserResponse> GetCurrentUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}