namespace SmartStayBLL
{
    public sealed class HostBookingAmountByCurrencyResponse
    {
        public string Currency { get; set; } =
            string.Empty;

        /*
         * Accommodation subtotal from bookings
         * that are currently confirmed.
         */
        public decimal ConfirmedBookingSubtotal
        {
            get;
            set;
        }

        /*
         * Accommodation subtotal from bookings
         * whose stays have been completed.
         */
        public decimal CompletedBookingSubtotal
        {
            get;
            set;
        }
    }
}