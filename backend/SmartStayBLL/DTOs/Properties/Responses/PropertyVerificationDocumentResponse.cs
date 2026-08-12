namespace SmartStayBLL
{
    public sealed class
        PropertyVerificationDocumentResponse
    {
        public Guid PropertyId { get; set; }

        public Guid DocumentId { get; set; }

        public string DocumentType { get; set; } =
            string.Empty;

        public int PagesCount { get; set; }

        public IReadOnlyList<
            PropertyVerificationDocumentPageResponse>
            Pages
        { get; set; } =
                Array.Empty<
                    PropertyVerificationDocumentPageResponse>();

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}