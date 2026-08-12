using SmartStayDAL;

namespace SmartStayBLL
{
    public interface IHostBookingService
    {
        Task<HostBookingsResponse>
            GetBookingsAsync(
                Guid hostUserId,
                int page,
                int pageSize,
                BookingStatus? status,
                CancellationToken cancellationToken = default);

        Task<HostBookingSummaryResponse>
            GetSummaryAsync(
                Guid hostUserId,
                CancellationToken cancellationToken = default);

        Task<HostBookingDetailsResponse>
            GetBookingByIdAsync(
                Guid hostUserId,
                Guid bookingId,
                CancellationToken cancellationToken = default);
    }
}