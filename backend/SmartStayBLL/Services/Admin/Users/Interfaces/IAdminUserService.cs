namespace SmartStayBLL
{
    public interface IAdminUserService
    {
        Task<AdminUsersResponse> GetUsersAsync(
            AdminUserSearchRequest request,
            CancellationToken cancellationToken = default);

        Task<AdminUserDetailsResponse> GetUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<AdminUserStatusResponse> DeactivateUserAsync(
            Guid currentAdminUserId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<AdminUserStatusResponse> ActivateUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}