namespace SmartStayBLL
{
    public sealed class HostBookingsResponse
    {
        public IReadOnlyList<HostBookingListItemResponse>
            Items
        { get; set; } =
                Array.Empty<HostBookingListItemResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public string? AppliedStatusFilter { get; set; }
    }
}