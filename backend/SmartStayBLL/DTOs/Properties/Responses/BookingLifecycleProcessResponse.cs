namespace SmartStayBLL
{
    public sealed class BookingLifecycleProcessResponse
    {
        public int ExpiredBookingsCount { get; set; }

        public int CompletedBookingsCount { get; set; }

        public int TotalUpdatedBookingsCount =>
            ExpiredBookingsCount
            +
            CompletedBookingsCount;

        public DateTimeOffset ProcessedAt { get; set; }
    }
}