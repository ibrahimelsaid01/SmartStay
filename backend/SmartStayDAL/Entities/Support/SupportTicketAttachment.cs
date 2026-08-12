namespace SmartStayDAL
{
    public sealed class SupportTicketAttachment
    {
        public Guid Id { get; set; }

        public Guid SupportTicketId { get; set; }

        public Guid UploadedByUserId { get; set; }

        public SupportTicketAttachmentType Type { get; set; } =
            SupportTicketAttachmentType.IssueEvidence;

        public string Url { get; set; } =
            string.Empty;

        public string PublicId { get; set; } =
            string.Empty;

        public string FileName { get; set; } =
            string.Empty;

        public string ContentType { get; set; } =
            string.Empty;

        public long FileSizeInBytes { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public SupportTicket SupportTicket { get; set; } =
            null!;

        public ApplicationUser UploadedByUser { get; set; } =
            null!;
    }
}