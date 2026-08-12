namespace SmartStayDAL
{
    public sealed class UserBookingRestriction
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public UserBookingRestrictionType Type { get; set; } =
            UserBookingRestrictionType.Warning;

        public UserBookingRestrictionStatus Status { get; set; } =
            UserBookingRestrictionStatus.Active;

        public string Reason { get; set; } =
            string.Empty;

        /*
         * Snapshot of how many cancellations triggered
         * this warning/restriction at creation time.
         */
        public int CancellationCountSnapshot { get; set; }

        /*
         * When the restriction starts.
         *
         * For warnings and admin flags, this is still useful
         * as the effective date of the record.
         */
        public DateTimeOffset RestrictedFrom { get; set; }

        /*
         * Required for TemporaryBookingRestriction.
         *
         * Optional for Warning and AdminReviewFlag.
         */
        public DateTimeOffset? RestrictedUntil { get; set; }

        public bool CreatedBySystem { get; set; } =
            true;

        public Guid? CreatedByAdminId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Guid? RemovedByAdminId { get; set; }

        public DateTimeOffset? RemovedAt { get; set; }

        public string? RemovalNote { get; set; }

        public ApplicationUser User { get; set; } =
            null!;

        public ApplicationUser? CreatedByAdmin { get; set; }

        public ApplicationUser? RemovedByAdmin { get; set; }
    }
}