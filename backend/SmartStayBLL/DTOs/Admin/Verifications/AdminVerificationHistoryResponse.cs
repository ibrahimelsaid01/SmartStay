namespace SmartStayBLL
{
    public sealed class AdminVerificationHistoryResponse
    {
        public Guid VerificationId { get; set; }

        public string VerificationType { get; set; } =
            string.Empty;

        public string ReferenceCode { get; set; } =
            string.Empty;

        public IReadOnlyList<AdminVerificationHistoryItemResponse>
            Items
        { get; set; }
            = new List<AdminVerificationHistoryItemResponse>();
    }

    public sealed class AdminVerificationHistoryItemResponse
    {
        public DateTimeOffset OccurredAt { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        /*
         * System, Host, Admin
         */
        public string ActorType { get; set; } =
            string.Empty;

        /*
         * Created, Submitted, Flagged, Approved, Rejected
         */
        public string EventType { get; set; } =
            string.Empty;

        public bool IsImportant { get; set; }
    }
}