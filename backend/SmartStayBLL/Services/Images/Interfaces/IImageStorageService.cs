namespace SmartStayBLL
{
    public interface IImageStorageService
    {
        Task<ImageUploadResult> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string folder,
            ImageAccessType accessType,
            CancellationToken cancellationToken = default);

        Task<ImageDeletionResult> DeleteAsync(
            string publicId,
            ImageAccessType accessType,
            CancellationToken cancellationToken = default);

        Task<ImageContentResult> DownloadAsync(
            string publicId,
            string format,
            ImageAccessType accessType,
            CancellationToken cancellationToken = default);
    }
}