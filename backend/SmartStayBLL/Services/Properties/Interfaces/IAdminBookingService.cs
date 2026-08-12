namespace SmartStayBLL
{
    public interface IAdminBookingService
    {
        Task<AdminBookingsResponse>
            GetBookingsAsync(
                AdminBookingSearchRequest request,
                CancellationToken cancellationToken = default);

        Task<AdminBookingSummaryResponse>
            GetSummaryAsync(
                CancellationToken cancellationToken = default);

        Task<AdminBookingDetailsResponse>
            GetBookingByIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default);
    }
}