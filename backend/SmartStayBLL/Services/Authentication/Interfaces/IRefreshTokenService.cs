namespace SmartStayBLL
{
    public interface IRefreshTokenService
    {
        Task<RefreshTokenIssueResult> CreateAsync(
            Guid userId,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<RefreshTokenRotationResult> RotateAsync(
            string rawToken,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task<bool> RevokeAsync(
            string rawToken,
            string? ipAddress,
            string reason,
            CancellationToken cancellationToken = default);

        Task RevokeAllForUserAsync(
            Guid userId,
            string? ipAddress,
            string reason,
            CancellationToken cancellationToken = default);
    }
}