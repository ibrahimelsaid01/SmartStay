namespace SmartStayBLL
{
    public interface IStripePaymentGateway
    {
        Task<StripePaymentIntentResult>
            CreatePaymentIntentAsync(
                CreateStripePaymentIntentRequest request,
                CancellationToken cancellationToken = default);

        Task<StripePaymentIntentResult>
            GetPaymentIntentAsync(
                string paymentIntentId,
                CancellationToken cancellationToken = default);

        Task<StripeRefundResult>
            CreateRefundAsync(
                CreateStripeRefundRequest request,
                CancellationToken cancellationToken = default);

        Task<StripeRefundResult>
            GetRefundAsync(
                string refundId,
                CancellationToken cancellationToken = default);
    }
}