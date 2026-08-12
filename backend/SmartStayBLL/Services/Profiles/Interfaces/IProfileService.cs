namespace SmartStayBLL
{
    public interface IProfileService
    {
        Task<UserProfileResponse> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<UserProfileResponse> UpdateAsync(
            Guid userId,
            UpdateUserProfileRequest request,
            CancellationToken cancellationToken = default);

        Task<UserProfileResponse> UploadImageAsync(
            Guid userId,
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<UserProfileResponse> DeleteImageAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}