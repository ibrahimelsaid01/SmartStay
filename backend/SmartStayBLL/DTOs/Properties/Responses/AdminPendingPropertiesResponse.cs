namespace SmartStayBLL
{
    public sealed class AdminPendingPropertiesResponse
    {
        public IReadOnlyList<
            AdminPendingPropertyItemResponse>
            Items
        { get; set; } =
                Array.Empty<
                    AdminPendingPropertyItemResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}