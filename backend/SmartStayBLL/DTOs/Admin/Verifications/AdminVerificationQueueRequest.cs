namespace SmartStayBLL
{
    public sealed class AdminVerificationQueueRequest
    {
        /*
         * Allowed values:
         * all, host, property
         */
        public string? Type { get; set; } = "all";

        public string? Search { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}