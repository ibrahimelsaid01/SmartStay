namespace SmartStayBLL
{
    public sealed class HostBookingSummaryResponse
    {
        public int TotalBookings { get; set; }

        public int PendingBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int CancelledBookings { get; set; }

        public int CompletedBookings { get; set; }

        public int ExpiredBookings { get; set; }

        /*
         * Pending bookings whose payment window is still
         * active, or Confirmed bookings, with a future
         * check-in date.
         */
        public int UpcomingBookings { get; set; }

        /*
         * Confirmed stays where:
         *
         * CheckInDate <= Today
         * CheckOutDate > Today
         */
        public int CurrentStays { get; set; }

        public IReadOnlyList<
            HostBookingAmountByCurrencyResponse>
            AmountsByCurrency
        { get; set; } =
                Array.Empty<
                    HostBookingAmountByCurrencyResponse>();
    }
}