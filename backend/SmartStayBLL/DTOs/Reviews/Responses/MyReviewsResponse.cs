namespace SmartStayBLL
{
    public sealed class MyReviewsResponse
    {
        public IReadOnlyList<UserReviewResponse> Items
        { get; set; } =
            Array.Empty<UserReviewResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}