namespace SmartStayDAL
{
    public sealed class PropertyVerificationDocumentPage
    {
        public Guid Id { get; set; }

        public Guid VerificationDocumentId { get; set; }

        public string PublicId { get; set; } =
            string.Empty;

        public string Format { get; set; } =
            string.Empty;

        public int PageNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public PropertyVerificationDocument
            VerificationDocument
        { get; set; } =
                null!;
    }
}