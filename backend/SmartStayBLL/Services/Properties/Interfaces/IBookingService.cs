using SmartStayDAL;

namespace SmartStayBLL
{
    public interface IBookingService
    {
        /*
         * Public booking operations.
         */

        Task<PropertyAvailabilityResponse>
            CheckAvailabilityAsync(
                Guid propertyId,
                BookingPeriodRequest request,
                CancellationToken cancellationToken = default);

        Task<BookingQuoteResponse>
            GetQuoteAsync(
                Guid propertyId,
                BookingPeriodRequest request,
                CancellationToken cancellationToken = default);

        /*
         * Authenticated booking creation.
         */

        Task<CreateBookingResponse>
            CreateAsync(
                Guid guestUserId,
                CreateBookingRequest request,
                CancellationToken cancellationToken = default);

        /*
         * Guest booking management.
         */

        Task<GuestBookingsResponse>
            GetGuestBookingsAsync(
                Guid guestUserId,
                int page,
                int pageSize,
                BookingStatus? status,
                CancellationToken cancellationToken = default);

        Task<GuestBookingDetailsResponse>
            GetGuestBookingByIdAsync(
                Guid guestUserId,
                Guid bookingId,
                CancellationToken cancellationToken = default);

        Task<GuestBookingConfirmationResponse>
            GetGuestBookingConfirmationAsync(
                Guid guestUserId,
                Guid bookingId,
                CancellationToken cancellationToken = default);

        Task<CancelBookingResponse>
            CancelGuestBookingAsync(
                Guid guestUserId,
                Guid bookingId,
                CancelBookingRequest request,
                CancellationToken cancellationToken = default);
    }
}