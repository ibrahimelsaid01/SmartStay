using Microsoft.Extensions.Options;
using Stripe;

namespace SmartStayBLL
{
    public sealed class StripePaymentGateway
        : IStripePaymentGateway
    {
        private const string ProviderName =
            "STRIPE";

        private readonly PaymentIntentService
            _paymentIntentService;

        private readonly RefundService
            _refundService;

        public StripePaymentGateway(
            IOptions<StripeSettings> stripeOptions)
        {
            ArgumentNullException.ThrowIfNull(
                stripeOptions);

            var settings =
                stripeOptions.Value;

            if (string.IsNullOrWhiteSpace(
                    settings.SecretKey))
            {
                throw new InvalidOperationException(
                    "Stripe secret key is not configured.");
            }

            var stripeClient =
                new StripeClient(
                    settings.SecretKey);

            _paymentIntentService =
                new PaymentIntentService(
                    stripeClient);

            _refundService =
                new RefundService(
                    stripeClient);
        }

        // =====================================================
        // PaymentIntent creation
        // =====================================================

        public async Task<StripePaymentIntentResult>
            CreatePaymentIntentAsync(
                CreateStripePaymentIntentRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ValidateCreatePaymentIntentRequest(
                request);

            var normalizedCurrency =
                StripeAmountConverter.NormalizeCurrency(
                    request.Currency);

            var amountInMinorUnit =
                StripeAmountConverter.ToMinorUnit(
                    request.Amount,
                    normalizedCurrency);

            var metadata =
                new Dictionary<string, string>
                {
                    ["paymentId"] =
                        request.PaymentId.ToString(),

                    ["bookingId"] =
                        request.BookingId.ToString(),

                    ["guestUserId"] =
                        request.GuestUserId.ToString()
                };

            if (request.BookingExpiresAt.HasValue)
            {
                metadata["bookingExpiresAt"] =
                    request.BookingExpiresAt.Value
                        .ToString("O");
            }

            var createOptions =
                new PaymentIntentCreateOptions
                {
                    Amount =
                        amountInMinorUnit,

                    Currency =
                        normalizedCurrency
                            .ToLowerInvariant(),

                    Description =
                        $"SmartStay booking {request.BookingId}",

                    AutomaticPaymentMethods =
                        new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled =
                                true
                        },

                    Metadata =
                        metadata
                };

            var stripeRequestOptions =
                new RequestOptions
                {
                    IdempotencyKey =
                        request.ProviderIdempotencyKey
                };

            try
            {
                var paymentIntent =
                    await _paymentIntentService
                        .CreateAsync(
                            createOptions,
                            stripeRequestOptions,
                            cancellationToken);

                return MapPaymentIntent(
                    paymentIntent);
            }
            catch (StripeException exception)
            {
                throw MapStripeException(
                    exception,
                    "Stripe could not create the payment intent.");
            }
        }

        // =====================================================
        // PaymentIntent retrieval
        // =====================================================

        public async Task<StripePaymentIntentResult>
            GetPaymentIntentAsync(
                string paymentIntentId,
                CancellationToken cancellationToken = default)
        {
            var normalizedPaymentIntentId =
                NormalizePaymentIntentId(
                    paymentIntentId);

            try
            {
                var paymentIntent =
                    await _paymentIntentService
                        .GetAsync(
                            normalizedPaymentIntentId,
                            null,
                            null,
                            cancellationToken);

                return MapPaymentIntent(
                    paymentIntent);
            }
            catch (StripeException exception)
            {
                throw MapStripeException(
                    exception,
                    "Stripe could not retrieve the payment intent.");
            }
        }

        // =====================================================
        // Refund creation
        // =====================================================

        public async Task<StripeRefundResult>
            CreateRefundAsync(
                CreateStripeRefundRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ValidateCreateRefundRequest(
                request);

            var normalizedCurrency =
                StripeAmountConverter.NormalizeCurrency(
                    request.Currency);

            var amountInMinorUnit =
                StripeAmountConverter.ToMinorUnit(
                    request.Amount,
                    normalizedCurrency);

            var metadata =
                new Dictionary<string, string>
                {
                    ["refundId"] =
                        request.RefundId.ToString(),

                    ["paymentId"] =
                        request.PaymentId.ToString(),

                    ["bookingId"] =
                        request.BookingId.ToString(),

                    ["guestUserId"] =
                        request.GuestUserId.ToString()
                };

            var createOptions =
                new RefundCreateOptions
                {
                    PaymentIntent =
                        request.ProviderPaymentId.Trim(),

                    Amount =
                        amountInMinorUnit,

                    /*
                     * Stripe requires lowercase currency code.
                     */
                    Currency =
                        normalizedCurrency
                            .ToLowerInvariant(),

                    Reason =
                        "requested_by_customer",

                    Metadata =
                        metadata
                };

            var stripeRequestOptions =
                new RequestOptions
                {
                    IdempotencyKey =
                        request.ProviderIdempotencyKey
                            .Trim()
                };

            try
            {
                var refund =
                    await _refundService
                        .CreateAsync(
                            createOptions,
                            stripeRequestOptions,
                            cancellationToken);

                return MapRefund(
                    refund);
            }
            catch (StripeException exception)
            {
                throw MapStripeException(
                    exception,
                    "Stripe could not create the refund.");
            }
        }

        // =====================================================
        // Refund retrieval
        // =====================================================

        public async Task<StripeRefundResult>
            GetRefundAsync(
                string refundId,
                CancellationToken cancellationToken = default)
        {
            var normalizedRefundId =
                NormalizeRefundId(
                    refundId);

            try
            {
                var refund =
                    await _refundService
                        .GetAsync(
                            normalizedRefundId,
                            null,
                            null,
                            cancellationToken);

                return MapRefund(
                    refund);
            }
            catch (StripeException exception)
            {
                throw MapStripeException(
                    exception,
                    "Stripe could not retrieve the refund.");
            }
        }

        // =====================================================
        // Mapping
        // =====================================================

        private static StripePaymentIntentResult
            MapPaymentIntent(
                PaymentIntent paymentIntent)
        {
            ArgumentNullException.ThrowIfNull(
                paymentIntent);

            if (string.IsNullOrWhiteSpace(
                    paymentIntent.Id))
            {
                throw new PaymentProviderException(
                    "Stripe returned a payment intent " +
                    "without an identifier.",
                    ProviderName);
            }

            if (string.IsNullOrWhiteSpace(
                    paymentIntent.ClientSecret))
            {
                throw new PaymentProviderException(
                    "Stripe returned a payment intent " +
                    "without a client secret.",
                    ProviderName);
            }

            return new StripePaymentIntentResult
            {
                PaymentIntentId =
                    paymentIntent.Id,

                ClientSecret =
                    paymentIntent.ClientSecret,

                Status =
                    paymentIntent.Status
                    ?? string.Empty,

                AmountInMinorUnit =
                    paymentIntent.Amount,

                Currency =
                    paymentIntent.Currency?
                        .ToUpperInvariant()
                    ?? string.Empty
            };
        }

        private static StripeRefundResult MapRefund(
            Refund refund)
        {
            ArgumentNullException.ThrowIfNull(
                refund);

            if (string.IsNullOrWhiteSpace(
                    refund.Id))
            {
                throw new PaymentProviderException(
                    "Stripe returned a refund without an identifier.",
                    ProviderName);
            }

            if (refund.Amount <= 0)
            {
                throw new PaymentProviderException(
                    "Stripe returned a refund with an invalid amount.",
                    ProviderName);
            }

            if (string.IsNullOrWhiteSpace(
                    refund.Currency))
            {
                throw new PaymentProviderException(
                    "Stripe returned a refund without currency.",
                    ProviderName);
            }

            return new StripeRefundResult
            {
                RefundId =
                    refund.Id,

                PaymentIntentId =
                    refund.PaymentIntentId,

                AmountInMinorUnit =
                    refund.Amount,

                Currency =
                    refund.Currency
                        .ToUpperInvariant(),

                Status =
                    refund.Status
                    ?? string.Empty,

                FailureReason =
                    refund.FailureReason,

                CreatedAt =
                    ToUtcDateTimeOffset(
                        refund.Created)
            };
        }

        // =====================================================
        // PaymentIntent validation
        // =====================================================

        private static void ValidateCreatePaymentIntentRequest(
            CreateStripePaymentIntentRequest request)
        {
            if (request.PaymentId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The local payment identifier is invalid.");
            }

            if (request.BookingId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The booking identifier is invalid.");
            }

            if (request.GuestUserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The guest user identifier is invalid.");
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentException(
                    "The payment amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.Currency))
            {
                throw new ArgumentException(
                    "The payment currency is required.");
            }

            ValidateProviderIdempotencyKey(
                request.ProviderIdempotencyKey);
        }

        private static string NormalizePaymentIntentId(
            string? paymentIntentId)
        {
            if (string.IsNullOrWhiteSpace(
                    paymentIntentId))
            {
                throw new ArgumentException(
                    "The Stripe payment intent identifier is required.");
            }

            var normalizedPaymentIntentId =
                paymentIntentId.Trim();

            if (!normalizedPaymentIntentId.StartsWith(
                    "pi_",
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The Stripe payment intent identifier is invalid.");
            }

            return normalizedPaymentIntentId;
        }

        // =====================================================
        // Refund validation
        // =====================================================

        private static void ValidateCreateRefundRequest(
            CreateStripeRefundRequest request)
        {
            if (request.RefundId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The local refund identifier is invalid.");
            }

            if (request.PaymentId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The local payment identifier is invalid.");
            }

            if (request.BookingId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The booking identifier is invalid.");
            }

            if (request.GuestUserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The guest user identifier is invalid.");
            }

            _ =
                NormalizePaymentIntentId(
                    request.ProviderPaymentId);

            if (request.Amount <= 0)
            {
                throw new ArgumentException(
                    "The refund amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.Currency))
            {
                throw new ArgumentException(
                    "The refund currency is required.");
            }

            ValidateProviderIdempotencyKey(
                request.ProviderIdempotencyKey);
        }

        private static string NormalizeRefundId(
            string? refundId)
        {
            if (string.IsNullOrWhiteSpace(
                    refundId))
            {
                throw new ArgumentException(
                    "The Stripe refund identifier is required.");
            }

            var normalizedRefundId =
                refundId.Trim();

            if (!normalizedRefundId.StartsWith(
                    "re_",
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The Stripe refund identifier is invalid.");
            }

            return normalizedRefundId;
        }

        private static void ValidateProviderIdempotencyKey(
            string? providerIdempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(
                    providerIdempotencyKey))
            {
                throw new ArgumentException(
                    "The Stripe idempotency key is required.");
            }

            if (providerIdempotencyKey.Trim().Length >
                255)
            {
                throw new ArgumentException(
                    "The Stripe idempotency key cannot exceed " +
                    "255 characters.");
            }
        }

        // =====================================================
        // Helpers
        // =====================================================

        private static DateTimeOffset ToUtcDateTimeOffset(
            DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return new DateTimeOffset(
                    dateTime);
            }

            return new DateTimeOffset(
                DateTime.SpecifyKind(
                    dateTime,
                    DateTimeKind.Utc));
        }

        private static PaymentProviderException
            MapStripeException(
                StripeException exception,
                string message)
        {
            var providerErrorCode =
                exception.StripeError?.Code;

            return new PaymentProviderException(
                message,
                ProviderName,
                providerErrorCode,
                exception);
        }
    }
}