namespace SmartStayBLL
{
    public sealed class AdminFinancialTransactionSearchRequest
    {
        public string? Search { get; set; }

        /*
         * Allowed values:
         * all, payment, refund
         */
        public string? Type { get; set; } = "all";

        public string? Currency { get; set; }

        /*
         * Payment statuses:
         * Pending, Succeeded, Failed, Cancelled,
         * PartiallyRefunded, Refunded
         *
         * Refund statuses:
         * Pending, RequiresAction, Succeeded,
         * Failed, Cancelled
         */
        public string? Status { get; set; }

        public DateTimeOffset? FromDate { get; set; }

        public DateTimeOffset? ToDate { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}