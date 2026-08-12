namespace SmartStayBLL
{
    public sealed class AdminBookingAmountByCurrencyResponse
    {
        public string Currency { get; set; } =
            string.Empty;

        public decimal ConfirmedGrossAmount { get; set; }

        public decimal ConfirmedServiceFees { get; set; }

        public decimal CompletedGrossAmount { get; set; }

        public decimal CompletedServiceFees { get; set; }
    }
}