namespace SmartStayBLL
{
    public sealed class AdminPropertyAmenityResponse
    {
        public Guid Id { get; set; }

        public string Code { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;

        public string Category { get; set; } =
            string.Empty;

        public string? IconKey { get; set; }

        public int DisplayOrder { get; set; }
    }
}