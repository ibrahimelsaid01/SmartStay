namespace SmartStayBLL
{
    public sealed class CreateSupportTicketRequest
    {
        public string Subject { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        /*
         * General, PaymentIssue, BookingIssue,
         * PropertyIssue, HostIssue, AccountIssue,
         * RefundIssue, TechnicalIssue, Other
         */
        public string Category { get; set; } =
            "General";

        /*
         * Low, Medium, High, Critical
         */
        public string Urgency { get; set; } =
            "Medium";

        public Guid? BookingId { get; set; }

        public Guid? PropertyId { get; set; }
    }
}