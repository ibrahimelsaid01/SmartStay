namespace SmartStayBLL
{
    public sealed class UserBookingRestrictionResponse
    {
        public Guid RestrictionId { get; set; }

        public Guid UserId { get; set; }

        public string Type { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string Reason { get; set; } =
            string.Empty;

        public int CancellationCountSnapshot { get; set; }

        public DateTimeOffset RestrictedFrom { get; set; }

        public DateTimeOffset? RestrictedUntil { get; set; }

        public bool CreatedBySystem { get; set; }

        public Guid? CreatedByAdminId { get; set; }

        public string? CreatedByAdminName { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Guid? RemovedByAdminId { get; set; }

        public string? RemovedByAdminName { get; set; }

        public DateTimeOffset? RemovedAt { get; set; }

        public string? RemovalNote { get; set; }
    }
}