namespace SmartStayBLL
{
    public interface IStripeWebhookService
    {
        Task ProcessAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default);
    }
}