namespace SmartStayBLL
{
    public sealed class WishListsResponse
    {
        public IReadOnlyList<WishListSummaryResponse> Items
        { get; set; } =
            Array.Empty<WishListSummaryResponse>();

        public int TotalCount { get; set; }
    }
}