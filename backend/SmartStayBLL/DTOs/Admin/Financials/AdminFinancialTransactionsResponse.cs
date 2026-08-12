namespace SmartStayBLL
{
    public sealed class AdminFinancialTransactionsResponse
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public IReadOnlyList<AdminFinancialTransactionItemResponse>
            Items
        { get; set; }
            = new List<AdminFinancialTransactionItemResponse>();
    }

    public sealed class AdminFinancialTransactionItemResponse
    {
        public Guid TransactionId { get; set; }

        public string ReferenceCode { get; set; } =
            string.Empty;

        /*
         * BookingPayment or Refund.
         */
        public string Type { get; set; } =
            string.Empty;

        /*
         * Incoming for payment.
         * Outgoing for refund.
         */
        public string Direction { get; set; } =
            string.Empty;

        public string Provider { get; set; } =
            string.Empty;

        public string? ProviderTransactionId { get; set; }

        public Guid? BookingId { get; set; }

        public Guid? PaymentId { get; set; }

        public Guid? RefundId { get; set; }

        public Guid? UserId { get; set; }

        public string UserName { get; set; } =
            string.Empty;

        public string? UserEmail { get; set; }

        public string? PropertyTitle { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        /*
         * Always positive.
         */
        public decimal Amount { get; set; }

        /*
         * Positive for incoming payments.
         * Negative for outgoing refunds.
         */
        public decimal SignedAmount { get; set; }

        public decimal PlatformFee { get; set; }

        public decimal RefundedAmount { get; set; }

        public decimal NetAmount { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string? FailureReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }
    }
}