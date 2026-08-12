namespace SmartStayBLL
{
    public sealed class HostPropertyStatusSummaryResponse
    {
        public int TotalProperties { get; set; }

        public int DraftProperties { get; set; }

        public int PendingProperties { get; set; }

        public int PublishedProperties { get; set; }

        public int RejectedProperties { get; set; }

        public int UnpublishedProperties { get; set; }
    }
}