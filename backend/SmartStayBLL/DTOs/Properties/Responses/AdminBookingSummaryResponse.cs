namespace SmartStayBLL
{
    public sealed class AdminBookingSummaryResponse
    {
        public int TotalBookings { get; set; }

        public int PendingBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int CancelledBookings { get; set; }

        public int CompletedBookings { get; set; }

        public int ExpiredBookings { get; set; }

        public int UpcomingBookings { get; set; }

        public int CurrentStays { get; set; }

        public IReadOnlyList<
            AdminBookingAmountByCurrencyResponse>
            AmountsByCurrency
        { get; set; } =
                Array.Empty<
                    AdminBookingAmountByCurrencyResponse>();
    }
}