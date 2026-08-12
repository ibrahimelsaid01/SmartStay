namespace SmartStayBLL
{
    public sealed class AdminVerificationQueueResponse
    {
        public DateTimeOffset GeneratedAt { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public AdminVerificationQueueSummaryResponse Summary { get; set; } =
            new();

        public IReadOnlyList<AdminVerificationQueueItemResponse>
            Items
        { get; set; }
            = new List<AdminVerificationQueueItemResponse>();
    }

    public sealed class AdminVerificationQueueSummaryResponse
    {
        public int TotalPending { get; set; }

        public int PendingHostApplications { get; set; }

        public int PendingPropertyVerifications { get; set; }

        /*
         * Items waiting more than 48 hours.
         */
        public int HighPriority { get; set; }

        public int ReviewedToday { get; set; }
    }

    public sealed class AdminVerificationQueueItemResponse
    {
        public Guid VerificationId { get; set; }

        /*
         * HostApplication or Property.
         */
        public string VerificationType { get; set; } =
            string.Empty;

        /*
         * UI-friendly reference.
         * Example:
         * HA-8472 or PV-8472
         */
        public string ReferenceCode { get; set; } =
            string.Empty;

        public string Title { get; set; } =
            string.Empty;

        public string Subtitle { get; set; } =
            string.Empty;

        public string ApplicantName { get; set; } =
            string.Empty;

        public string ApplicantEmail { get; set; } =
            string.Empty;

        public string? ApplicantPhoneNumber { get; set; }

        public string? ApplicantImageUrl { get; set; }

        public string? Location { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public bool IsHighPriority { get; set; }

        public int DocumentsCount { get; set; }

        public int MissingDocumentsCount { get; set; }

        public bool HasRequiredDocuments { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public string DetailsEndpoint { get; set; } =
            string.Empty;

        public string ApproveEndpoint { get; set; } =
            string.Empty;

        public string RejectEndpoint { get; set; } =
            string.Empty;

        public string HistoryEndpoint { get; set; } =
            string.Empty;
    }
}