namespace SmartStayDAL
{
    public sealed class SupportTicketMessage
    {
        public Guid Id { get; set; }

        public Guid SupportTicketId { get; set; }

        public Guid SenderUserId { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public bool IsAdminMessage { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public SupportTicket SupportTicket { get; set; } =
            null!;

        public ApplicationUser SenderUser { get; set; } =
            null!;
    }
}