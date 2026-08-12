namespace SmartStayDAL
{
    public sealed class BookingPayout
    {
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }

        public Guid BookingPaymentId { get; set; }

        public Guid HostProfileId { get; set; }

        /*
         * Host payout amount.
         *
         * This should normally be based on Booking.Subtotal,
         * not Booking.TotalAmount, because TotalAmount can
         * include guest-facing platform service fees.
         */
        public decimal Amount { get; set; }

        public string Currency { get; set; } =
            "EGP";

        public BookingPayoutStatus Status { get; set; } =
            BookingPayoutStatus.Pending;

        /*
         * The time when the payout can become available
         * if there is no active complaint or dispute.
         */
        public DateTimeOffset? AvailableAt { get; set; }

        public DateTimeOffset? HeldAt { get; set; }

        public string? HoldReason { get; set; }

        public DateTimeOffset? ReleasedAt { get; set; }

        public string? ReleaseNote { get; set; }

        public DateTimeOffset? PaidAt { get; set; }

        public DateTimeOffset? BlockedAt { get; set; }

        public string? BlockReason { get; set; }

        public DateTimeOffset? RefundedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Booking Booking { get; set; } =
            null!;

        public BookingPayment BookingPayment { get; set; } =
            null!;

        public HostProfile HostProfile { get; set; } =
            null!;
    }
}