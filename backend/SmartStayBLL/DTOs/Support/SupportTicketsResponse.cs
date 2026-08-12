namespace SmartStayBLL
{
    public sealed class SupportTicketsResponse
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public IReadOnlyList<SupportTicketListItemResponse>
            Items
        { get; set; }
            = new List<SupportTicketListItemResponse>();
    }

    public sealed class SupportTicketListItemResponse
    {
        public Guid TicketId { get; set; }

        public string ReferenceCode { get; set; } =
            string.Empty;

        public string Subject { get; set; } =
            string.Empty;

        public string Category { get; set; } =
            string.Empty;

        public string Urgency { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public Guid CreatedByUserId { get; set; }

        public string CreatedByName { get; set; } =
            string.Empty;

        public string? CreatedByEmail { get; set; }

        public Guid? BookingId { get; set; }

        public Guid? PropertyId { get; set; }

        public string? PropertyTitle { get; set; }

        public int MessagesCount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? ResolvedAt { get; set; }
    }
}