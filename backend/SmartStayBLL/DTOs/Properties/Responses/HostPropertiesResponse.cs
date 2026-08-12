namespace SmartStayBLL
{
    public sealed class HostPropertiesResponse
    {
        public IReadOnlyList<HostPropertyListItemResponse>
            Items
        { get; set; } =
                Array.Empty<HostPropertyListItemResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public string? AppliedStatusFilter { get; set; }
    }
}