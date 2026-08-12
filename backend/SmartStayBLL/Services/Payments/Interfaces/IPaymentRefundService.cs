namespace SmartStayBLL
{
    public interface IPaymentRefundService
    {
        Task<PaymentRefundResponse>
            CreateBookingCancellationRefundAsync(
                Guid guestUserId,
                Guid bookingId,
                decimal refundAmount,
                CancellationToken cancellationToken = default);

        Task<PaymentRefundResponse>
            CreateSupportTicketRefundAsync(
                Guid adminUserId,
                Guid supportTicketId,
                decimal refundAmount,
                CancellationToken cancellationToken = default);
    }
}