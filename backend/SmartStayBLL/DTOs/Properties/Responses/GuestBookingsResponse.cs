namespace SmartStayBLL
{
    public sealed class GuestBookingsResponse
    {
        public IReadOnlyList<GuestBookingListItemResponse>
            Items
        { get; set; } =
                Array.Empty<GuestBookingListItemResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public string? AppliedStatusFilter { get; set; }
    }
}