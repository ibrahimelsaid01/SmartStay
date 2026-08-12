namespace SmartStayBLL
{
    public sealed class PublicPropertyImageResponse
    {
        public Guid Id { get; set; }

        public string Url { get; set; } =
            string.Empty;

        public bool IsCover { get; set; }

        public int DisplayOrder { get; set; }
    }
}