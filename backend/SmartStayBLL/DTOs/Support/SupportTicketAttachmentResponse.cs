namespace SmartStayBLL
{
    public sealed class SupportTicketAttachmentResponse
    {
        public Guid AttachmentId { get; set; }

        public Guid UploadedByUserId { get; set; }

        public string UploadedByName { get; set; } =
            string.Empty;

        public string? UploadedByEmail { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public string Url { get; set; } =
            string.Empty;

        public string FileName { get; set; } =
            string.Empty;

        public string ContentType { get; set; } =
            string.Empty;

        public long FileSizeInBytes { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}