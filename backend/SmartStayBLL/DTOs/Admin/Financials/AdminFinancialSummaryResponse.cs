namespace SmartStayBLL
{
    public sealed class AdminFinancialSummaryResponse
    {
        public DateTimeOffset GeneratedAt { get; set; }

        public IReadOnlyList<AdminFinancialCurrencySummaryResponse>
            Currencies
        { get; set; }
            = new List<AdminFinancialCurrencySummaryResponse>();
    }

    public sealed class AdminFinancialCurrencySummaryResponse
    {
        public string Currency { get; set; } =
            string.Empty;

        public int TotalPaymentAttempts { get; set; }

        public int PendingPayments { get; set; }

        public int SuccessfulPayments { get; set; }

        public int FailedPayments { get; set; }

        public int CancelledPayments { get; set; }

        public int PartiallyRefundedPayments { get; set; }

        public int FullyRefundedPayments { get; set; }

        public decimal GrossVolume { get; set; }

        public decimal PlatformRevenue { get; set; }

        public decimal TotalRefundedAmount { get; set; }

        public decimal NetVolume { get; set; }

        public int TotalRefundRequests { get; set; }

        public int PendingRefundRequests { get; set; }

        public int SuccessfulRefundRequests { get; set; }

        public int FailedRefundRequests { get; set; }

        public decimal SuccessRatePercentage { get; set; }

        /*
         * Not implemented yet.
         * This remains zero until we build Host Payouts.
         */
        public int PendingPayoutRequests { get; set; }

        /*
         * Not implemented yet.
         * This remains zero until we build Host Payouts.
         */
        public decimal PendingPayoutAmount { get; set; }
    }
}