namespace SmartStayBLL
{
    public sealed class WishListDetailsResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public IReadOnlyList<WishListItemResponse> Items
        { get; set; } =
            Array.Empty<WishListItemResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}