namespace SmartStayBLL
{
    public sealed class PropertySubmissionResponse
    {
        public Guid Id { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset SubmittedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}