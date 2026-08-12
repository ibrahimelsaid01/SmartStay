namespace SmartStayBLL
{
    internal static class StripeAmountConverter
    {
        private static readonly HashSet<string>
            ZeroDecimalCurrencies =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "BIF",
                    "CLP",
                    "DJF",
                    "GNF",
                    "JPY",
                    "KMF",
                    "KRW",
                    "MGA",
                    "PYG",
                    "RWF",
                    "VND",
                    "VUV",
                    "XAF",
                    "XOF",
                    "XPF"
                };

        /*
         * Stripe treats these currencies as two-decimal
         * currencies for charge API compatibility, but
         * fractional whole units are not accepted.
         */
        private static readonly HashSet<string>
            WholeUnitCurrenciesEncodedWithTwoDecimals =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "ISK",
                    "UGX"
                };

        public static long ToMinorUnit(
            decimal amount,
            string currency)
        {
            if (amount <= 0)
            {
                throw new ArgumentException(
                    "The payment amount must be greater than zero.");
            }

            var normalizedCurrency =
                NormalizeCurrency(
                    currency);

            if (WholeUnitCurrenciesEncodedWithTwoDecimals.Contains(
                    normalizedCurrency)
                &&
                amount != decimal.Truncate(
                    amount))
            {
                throw new ArgumentException(
                    $"Currency '{normalizedCurrency}' " +
                    "does not support fractional whole units.");
            }

            var multiplier =
                GetCurrencyMultiplier(
                    normalizedCurrency);

            var amountInMinorUnit =
                amount * multiplier;

            if (amountInMinorUnit !=
                decimal.Truncate(
                    amountInMinorUnit))
            {
                throw new ArgumentException(
                    $"The amount contains too many decimal " +
                    $"places for currency '{normalizedCurrency}'.");
            }

            try
            {
                return checked((long)amountInMinorUnit);
            }
            catch (OverflowException exception)
            {
                throw new ArgumentException(
                    "The payment amount is too large.",
                    exception);
            }
        }

        public static decimal FromMinorUnit(
            long amountInMinorUnit,
            string currency)
        {
            if (amountInMinorUnit <= 0)
            {
                throw new ArgumentException(
                    "The Stripe amount must be greater than zero.");
            }

            var normalizedCurrency =
                NormalizeCurrency(
                    currency);

            var multiplier =
                GetCurrencyMultiplier(
                    normalizedCurrency);

            return decimal.Round(
                amountInMinorUnit / multiplier,
                2,
                MidpointRounding.AwayFromZero);
        }

        public static string NormalizeCurrency(
            string? currency)
        {
            return CurrencyCodeNormalizer
                .NormalizeForPayment(
                    currency);
        }

        private static decimal GetCurrencyMultiplier(
            string normalizedCurrency)
        {
            return ZeroDecimalCurrencies.Contains(
                    normalizedCurrency)
                ? 1m
                : 100m;
        }
    }
}