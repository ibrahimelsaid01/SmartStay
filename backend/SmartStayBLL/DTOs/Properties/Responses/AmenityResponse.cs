namespace SmartStayBLL
{
    public sealed class AmenityResponse
    {
        public Guid Id { get; set; }

        public string Code { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;

        public string Category { get; set; } =
            string.Empty;

        public string IconKey { get; set; } =
            string.Empty;

        public int DisplayOrder { get; set; }
    }
}