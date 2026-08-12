namespace SmartStayBLL
{
    public sealed class UploadSupportTicketAttachmentResponse
    {
        public Guid TicketId { get; set; }

        public Guid AttachmentId { get; set; }

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

        public string Message { get; set; } =
            string.Empty;
    }
}