namespace SmartStayBLL
{
    public sealed class
        AdminPropertyVerificationDocumentResponse
    {
        public Guid Id { get; set; }

        public string DocumentType { get; set; } =
            string.Empty;

        public int PagesCount { get; set; }

        public IReadOnlyList<
            AdminPropertyVerificationPageResponse>
            Pages
        { get; set; } =
                Array.Empty<
                    AdminPropertyVerificationPageResponse>();

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}