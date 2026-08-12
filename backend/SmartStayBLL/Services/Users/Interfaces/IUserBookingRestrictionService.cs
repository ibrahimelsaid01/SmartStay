namespace SmartStayBLL
{
    public interface IUserBookingRestrictionService
    {
        Task EnsureUserCanCreateBookingAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<UserBookingRestrictionResponse?> GetActiveBookingRestrictionAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<UserBookingRestrictionResponse?> EvaluateGuestCancellationAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserBookingRestrictionResponse>> GetUserRestrictionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<UserBookingRestrictionResponse> ApplyTemporaryBookingRestrictionAsync(
            Guid adminUserId,
            Guid adminReviewFlagId,
            ApplyTemporaryBookingRestrictionRequest request,
            CancellationToken cancellationToken = default);

        Task<UserBookingRestrictionResponse> RemoveRestrictionAsync(
            Guid adminUserId,
            Guid restrictionId,
            RemoveUserBookingRestrictionRequest request,
            CancellationToken cancellationToken = default);
    }
}