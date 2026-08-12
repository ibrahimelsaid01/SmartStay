namespace SmartStayDAL
{
    public sealed class PropertyVerificationDocument
    {
        public Guid Id { get; set; }

        public Guid PropertyId { get; set; }

        public PropertyVerificationDocumentType
            DocumentType
        { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Property Property { get; set; } =
            null!;

        public ICollection<
            PropertyVerificationDocumentPage>
            Pages
        { get; set; } =
                new List<
                    PropertyVerificationDocumentPage>();
    }
}