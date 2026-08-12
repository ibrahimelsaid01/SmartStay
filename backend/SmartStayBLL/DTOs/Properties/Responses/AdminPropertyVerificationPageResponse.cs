namespace SmartStayBLL
{
    public sealed class
        AdminPropertyVerificationPageResponse
    {
        public Guid Id { get; set; }

        public int PageNumber { get; set; }

        public string Format { get; set; } =
            string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}