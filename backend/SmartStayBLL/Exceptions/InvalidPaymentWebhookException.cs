namespace SmartStayBLL
{
    public sealed class InvalidPaymentWebhookException
        : Exception
    {
        public InvalidPaymentWebhookException(
            string message,
            Exception? innerException = null)
            : base(
                message,
                innerException)
        {
        }
    }
}