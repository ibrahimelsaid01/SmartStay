namespace SmartStayBLL
{
    public interface IHostApplicationService
    {
        Task<HostApplicationResponse> CreateDraftAsync(
            Guid userId,
            CreateHostApplicationRequest request,
            CancellationToken cancellationToken = default);

        Task<HostApplicationResponse> GetCurrentAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<HostApplicationResponse> UpdateCurrentAsync(
            Guid userId,
            UpdateHostApplicationRequest request,
            CancellationToken cancellationToken = default);

        Task<HostApplicationResponse> UploadProfileImageAsync(
            Guid userId,
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<HostApplicationResponse> UploadNationalIdAsync(
            Guid userId,
            Stream frontFileStream,
            string frontFileName,
            string frontContentType,
            Stream backFileStream,
            string backFileName,
            string backContentType,
            CancellationToken cancellationToken = default);

        Task<HostApplicationResponse> SubmitCurrentAsync(
        Guid userId,
        CancellationToken cancellationToken = default);


    }
}