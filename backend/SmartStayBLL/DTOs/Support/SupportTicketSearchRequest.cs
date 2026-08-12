namespace SmartStayBLL
{
    public sealed class SupportTicketSearchRequest
    {
        public string? Search { get; set; }

        /*
         * Open, InProgress, Resolved, Closed
         */
        public string? Status { get; set; }

        /*
         * General, PaymentIssue, BookingIssue,
         * PropertyIssue, HostIssue, AccountIssue,
         * RefundIssue, TechnicalIssue, Other
         */
        public string? Category { get; set; }

        /*
         * Low, Medium, High, Critical
         */
        public string? Urgency { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}