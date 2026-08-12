namespace SmartStayBLL
{
    public sealed class AdminBookingsResponse
    {
        public IReadOnlyList<AdminBookingListItemResponse>
            Items
        { get; set; } =
                Array.Empty<AdminBookingListItemResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public string? AppliedStatusFilter { get; set; }

        public Guid? AppliedPropertyIdFilter { get; set; }

        public Guid? AppliedGuestUserIdFilter { get; set; }

        public Guid? AppliedHostUserIdFilter { get; set; }

        public DateOnly? AppliedCheckInFromFilter { get; set; }

        public DateOnly? AppliedCheckInToFilter { get; set; }
    }
}