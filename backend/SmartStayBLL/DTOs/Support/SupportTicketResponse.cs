namespace SmartStayBLL
{
    public sealed class SupportTicketResponse
    {
        public Guid TicketId { get; set; }

        public string ReferenceCode { get; set; } =
            string.Empty;

        public Guid CreatedByUserId { get; set; }

        public string CreatedByName { get; set; } =
            string.Empty;

        public string? CreatedByEmail { get; set; }

        public Guid? BookingId { get; set; }

        public Guid? PropertyId { get; set; }

        public string? PropertyTitle { get; set; }

        public string Subject { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        public string Category { get; set; } =
            string.Empty;

        public string Urgency { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string DecisionStatus { get; set; } =
            string.Empty;

        public string DecisionAction { get; set; } =
            string.Empty;

        public string? DecisionNote { get; set; }

        public DateTimeOffset? DecidedAt { get; set; }

        public Guid? DecidedByAdminId { get; set; }

        public string? DecidedByAdminName { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? ResolvedAt { get; set; }

        public string? ResolutionNote { get; set; }

        public IReadOnlyList<SupportTicketMessageResponse>
            Messages
        { get; set; }
            = new List<SupportTicketMessageResponse>();

        public IReadOnlyList<SupportTicketAttachmentResponse>
            Attachments
        { get; set; }
            = new List<SupportTicketAttachmentResponse>();
    }

    public sealed class SupportTicketMessageResponse
    {
        public Guid MessageId { get; set; }

        public Guid SenderUserId { get; set; }

        public string SenderName { get; set; } =
            string.Empty;

        public string? SenderEmail { get; set; }

        public bool IsAdminMessage { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}