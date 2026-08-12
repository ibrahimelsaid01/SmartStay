namespace SmartStayBLL
{
    public static class CurrencyCodeNormalizer
    {
        private static readonly HashSet<string>
            SupportedCurrencies =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "EGP",
                    "USD",
                    "EUR"
                };

        public static string NormalizeForStorage(
            string? currency)
        {
            var normalizedCurrency =
                NormalizeBasic(
                    currency);

            /*
             * Common wrong value that appeared in the database.
             *
             * Egypt's valid ISO currency code is EGP, not EGY.
             */
            if (normalizedCurrency == "EGY")
            {
                return "EGP";
            }

            if (!SupportedCurrencies.Contains(
                    normalizedCurrency))
            {
                throw new ArgumentException(
                    $"Currency '{normalizedCurrency}' is not supported. " +
                    "Supported currencies are: EGP, USD, EUR.");
            }

            return normalizedCurrency;
        }

        public static string NormalizeForPayment(
            string? currency)
        {
            /*
             * For now, payment currency follows the same
             * supported currency list used for storage.
             */
            return NormalizeForStorage(
                currency);
        }

        public static string NormalizeForDisplay(
            string? currency)
        {
            if (string.IsNullOrWhiteSpace(
                    currency))
            {
                return "EGP";
            }

            var normalizedCurrency =
                currency.Trim()
                    .ToUpperInvariant();

            return normalizedCurrency == "EGY"
                ? "EGP"
                : normalizedCurrency;
        }

        private static string NormalizeBasic(
            string? currency)
        {
            var normalizedCurrency =
                currency?
                    .Trim()
                    .ToUpperInvariant()
                ?? string.Empty;

            if (normalizedCurrency.Length != 3
                ||
                normalizedCurrency.Any(
                    character =>
                        !char.IsAsciiLetter(
                            character)))
            {
                throw new ArgumentException(
                    "Currency must contain exactly 3 English letters.");
            }

            return normalizedCurrency;
        }
    }
}