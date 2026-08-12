namespace SmartStayBLL
{
    public interface IAmenityService
    {
        Task<IReadOnlyList<AmenityResponse>>
            GetActiveAmenitiesAsync(
                CancellationToken cancellationToken =
                    default);
    }
}