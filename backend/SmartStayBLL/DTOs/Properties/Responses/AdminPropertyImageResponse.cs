namespace SmartStayBLL
{
    public sealed class AdminPropertyImageResponse
    {
        public Guid Id { get; set; }

        public string Url { get; set; } =
            string.Empty;

        public string Format { get; set; } =
            string.Empty;

        public bool IsCover { get; set; }

        public int DisplayOrder { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}