namespace SmartStayBLL
{
    public sealed class PublicPropertiesResponse
    {
        public IReadOnlyList<PublicPropertyListItemResponse>
            Items
        { get; set; } =
                Array.Empty<PublicPropertyListItemResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}