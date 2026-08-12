namespace SmartStayBLL
{
    public interface IPaymentService
    {
        Task<StartPaymentResponse>
            StartPaymentAsync(
                Guid guestUserId,
                StartPaymentRequest request,
                string idempotencyKey,
                CancellationToken cancellationToken = default);

        Task<PaymentStatusResponse>
            GetPaymentStatusAsync(
                Guid guestUserId,
                Guid paymentId,
                CancellationToken cancellationToken = default);
    }
}