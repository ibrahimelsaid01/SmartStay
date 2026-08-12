namespace SmartStayDAL
{
    public sealed class SupportTicket
    {
        public Guid Id { get; set; }

        public Guid CreatedByUserId { get; set; }

        public Guid? BookingId { get; set; }

        public Guid? PropertyId { get; set; }

        public string Subject { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        public SupportTicketCategory Category { get; set; } =
            SupportTicketCategory.General;

        public SupportTicketUrgency Urgency { get; set; } =
            SupportTicketUrgency.Medium;

        public SupportTicketStatus Status { get; set; } =
            SupportTicketStatus.Open;

        public SupportTicketDecisionStatus DecisionStatus { get; set; } =
            SupportTicketDecisionStatus.NoDecision;

        public SupportTicketDecisionAction DecisionAction { get; set; } =
            SupportTicketDecisionAction.NoAction;

        public string? DecisionNote { get; set; }

        public DateTimeOffset? DecidedAt { get; set; }

        public Guid? DecidedByAdminId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? ResolvedAt { get; set; }

        public Guid? ResolvedByAdminId { get; set; }

        public string? ResolutionNote { get; set; }

        public ApplicationUser CreatedByUser { get; set; } =
            null!;

        public Booking? Booking { get; set; }

        public Property? Property { get; set; }

        public ApplicationUser? ResolvedByAdmin { get; set; }

        public ApplicationUser? DecidedByAdmin { get; set; }

        public ICollection<SupportTicketMessage> Messages { get; set; } =
            new List<SupportTicketMessage>();

        public ICollection<SupportTicketAttachment> Attachments { get; set; } =
            new List<SupportTicketAttachment>();
    }
}