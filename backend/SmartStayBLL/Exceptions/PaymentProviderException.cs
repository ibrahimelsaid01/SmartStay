namespace SmartStayBLL
{
    public sealed class PaymentProviderException
        : Exception
    {
        public PaymentProviderException(
            string message,
            string provider,
            string? providerErrorCode = null,
            Exception? innerException = null)
            : base(
                message,
                innerException)
        {
            Provider =
                provider;

            ProviderErrorCode =
                providerErrorCode;
        }

        public string Provider { get; }

        public string? ProviderErrorCode { get; }
    }
}