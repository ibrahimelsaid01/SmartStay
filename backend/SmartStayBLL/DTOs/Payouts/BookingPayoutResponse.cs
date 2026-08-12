namespace SmartStayBLL
{
    public sealed class BookingPayoutResponse
    {
        public Guid PayoutId { get; set; }

        public Guid BookingId { get; set; }

        public Guid BookingPaymentId { get; set; }

        public Guid HostProfileId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

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
    }
}