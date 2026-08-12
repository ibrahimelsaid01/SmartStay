namespace SmartStayBLL
{
    public interface IBookingPayoutService
    {
        Task<BookingPayoutResponse>
            CreatePendingPayoutForSucceededPaymentAsync(
                Guid bookingPaymentId,
                CancellationToken cancellationToken = default);

        Task<BookingPayoutResponse?>
            GetByBookingIdAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default);

        Task<BookingPayoutResponse>
            HoldPayoutForBookingAsync(
                Guid bookingId,
                string reason,
                CancellationToken cancellationToken = default);

        Task<BookingPayoutResponse>
            ReleasePayoutForBookingAsync(
                Guid bookingId,
                string? releaseNote,
                CancellationToken cancellationToken = default);

        Task<BookingPayoutResponse>
            BlockPayoutForBookingAsync(
                Guid bookingId,
                string reason,
                CancellationToken cancellationToken = default);

        Task<BookingPayoutResponse>
            MarkPayoutRefundedForBookingAsync(
                Guid bookingId,
                string? refundNote,
                CancellationToken cancellationToken = default);

        Task<BookingPayoutResponse>
            ReconcilePartialRefundForBookingAsync(
                Guid bookingId,
                Guid paymentRefundId,
                string? reconciliationNote,
                CancellationToken cancellationToken = default);
    }
}