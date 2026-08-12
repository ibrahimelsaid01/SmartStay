using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminBookingSearchRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public BookingStatus? Status { get; set; }

        public Guid? PropertyId { get; set; }

        public Guid? GuestUserId { get; set; }

        public Guid? HostUserId { get; set; }

        /*
         * Filter by the booking check-in date.
         */
        public DateOnly? CheckInFrom { get; set; }

        public DateOnly? CheckInTo { get; set; }
    }
}