namespace SmartStayBLL
{
    public interface IPublicPropertyService
    {
        Task<PublicPropertiesResponse> SearchAsync(
            PublicPropertySearchRequest request,
            CancellationToken cancellationToken = default);

        Task<PublicPropertyDetailsResponse> GetByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default);
    }
}