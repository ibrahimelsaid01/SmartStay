namespace SmartStayBLL
{
    public sealed class PropertyReviewsResponse
    {
        public Guid PropertyId { get; set; }

        public IReadOnlyList<PublicReviewResponse> Items
        { get; set; } =
            Array.Empty<PublicReviewResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}