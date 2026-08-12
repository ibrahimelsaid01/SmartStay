namespace SmartStayBLL
{
    public sealed class AdminReviewsResponse
    {
        public IReadOnlyList<AdminReviewListItemResponse> Items
        { get; set; } =
            Array.Empty<AdminReviewListItemResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}