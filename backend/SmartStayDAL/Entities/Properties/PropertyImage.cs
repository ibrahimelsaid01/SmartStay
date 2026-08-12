namespace SmartStayDAL
{
    public sealed class PropertyImage
    {
        public Guid Id { get; set; }

        public Guid PropertyId { get; set; }

        public string Url { get; set; } =
            string.Empty;

        public string PublicId { get; set; } =
            string.Empty;

        public string Format { get; set; } =
            string.Empty;

        public bool IsCover { get; set; }

        public int DisplayOrder { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Property Property { get; set; } =
            null!;
    }
}