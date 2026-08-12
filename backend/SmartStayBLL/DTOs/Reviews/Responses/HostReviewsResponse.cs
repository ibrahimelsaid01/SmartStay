namespace SmartStayBLL
{
    public sealed class HostReviewsResponse
    {
        public IReadOnlyList<HostReviewResponse> Items
        { get; set; } =
            Array.Empty<HostReviewResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}