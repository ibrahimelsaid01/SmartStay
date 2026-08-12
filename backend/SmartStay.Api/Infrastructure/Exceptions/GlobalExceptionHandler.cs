using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;

namespace SmartStay.Api
{
    public sealed class GlobalExceptionHandler
        : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler>
            _logger;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(
                logger);

            _logger =
                logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                httpContext);

            ArgumentNullException.ThrowIfNull(
                exception);

            var error =
                MapException(
                    exception);

            LogException(
                httpContext,
                exception,
                error);

            AddResponseHeaders(
                httpContext,
                exception);

            var problemDetails =
                new ProblemDetails
                {
                    Status =
                        error.StatusCode,

                    Title =
                        error.Title,

                    Detail =
                        error.Detail,

                    Instance =
                        httpContext.Request.Path
                };

            problemDetails.Extensions["code"] =
                error.Code;

            problemDetails.Extensions["traceId"] =
                httpContext.TraceIdentifier;

            /*
             * Expose safe provider information for debugging.
             *
             * Never expose:
             *
             * Stripe secret keys
             * Client secrets
             * Raw Stripe responses
             */
            if (exception is PaymentProviderException
                paymentProviderException)
            {
                problemDetails.Extensions["provider"] =
                    paymentProviderException.Provider;

                if (!string.IsNullOrWhiteSpace(
                        paymentProviderException
                            .ProviderErrorCode))
                {
                    problemDetails.Extensions[
                        "providerErrorCode"] =
                        paymentProviderException
                            .ProviderErrorCode;
                }
            }

            httpContext.Response.StatusCode =
                error.StatusCode;

            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);

            return true;
        }

        private void LogException(
            HttpContext httpContext,
            Exception exception,
            ErrorDetails error)
        {
            /*
             * Provider errors and unexpected server errors
             * are logged as errors.
             */
            if (error.StatusCode >= 500)
            {
                _logger.LogError(
                    exception,
                    "Request failed with status {StatusCode}. " +
                    "ErrorCode: {ErrorCode}. " +
                    "TraceId: {TraceId}",
                    error.StatusCode,
                    error.Code,
                    httpContext.TraceIdentifier);

                return;
            }

            /*
             * Client and business-rule failures are expected
             * application outcomes, so they are warnings.
             */
            _logger.LogWarning(
                exception,
                "Request failed with status {StatusCode}. " +
                "ErrorCode: {ErrorCode}. " +
                "TraceId: {TraceId}",
                error.StatusCode,
                error.Code,
                httpContext.TraceIdentifier);
        }

        private static void AddResponseHeaders(
            HttpContext httpContext,
            Exception exception)
        {
            if (exception is OtpCooldownException cooldown)
            {
                httpContext.Response.Headers["Retry-After"] =
                    cooldown.RetryAfterSeconds.ToString();
            }
        }

        private static ErrorDetails MapException(
            Exception exception)
        {
            return exception switch
            {
                OtpCooldownException cooldown =>
                    new ErrorDetails(
                        StatusCodes.Status429TooManyRequests,
                        "Too many requests",
                        "OTP_COOLDOWN",
                        cooldown.Message),

                /*
                 * Errors returned by Stripe or another
                 * external payment provider.
                 */
                InvalidPaymentWebhookException invalidWebhook =>
                    new ErrorDetails(
                        StatusCodes.Status400BadRequest,
                        "Invalid payment webhook",
                        "INVALID_PAYMENT_WEBHOOK",
                        invalidWebhook.Message),

                PaymentProviderException paymentProviderException =>
                    new ErrorDetails(
                        StatusCodes.Status502BadGateway,
                        "Payment provider error",
                        "PAYMENT_PROVIDER_ERROR",
                        paymentProviderException.Message),

                HostApplicationAlreadyExistsException =>
                    new ErrorDetails(
                        StatusCodes.Status409Conflict,
                        "Host application already exists",
                        "HOST_APPLICATION_ALREADY_EXISTS",
                        exception.Message),

                HostApplicationNotEditableException =>
                    new ErrorDetails(
                        StatusCodes.Status409Conflict,
                        "Host application cannot be edited",
                        "HOST_APPLICATION_NOT_EDITABLE",
                        exception.Message),

                HostApplicationCannotBeSubmittedException =>
                    new ErrorDetails(
                        StatusCodes.Status409Conflict,
                        "Host application cannot be submitted",
                        "HOST_APPLICATION_CANNOT_BE_SUBMITTED",
                        exception.Message),

                HostApplicationIncompleteException =>
                    new ErrorDetails(
                        StatusCodes.Status400BadRequest,
                        "Host application is incomplete",
                        "HOST_APPLICATION_INCOMPLETE",
                        exception.Message),

                HostApplicationNotPendingException =>
                    new ErrorDetails(
                        StatusCodes.Status409Conflict,
                        "Host application cannot be reviewed",
                        "HOST_APPLICATION_NOT_PENDING",
                        exception.Message),

                PropertyNotEditableException =>
                    new ErrorDetails(
                        StatusCodes.Status409Conflict,
                        "Property cannot be edited",
                        "PROPERTY_NOT_EDITABLE",
                        exception.Message),

                UnauthorizedAccessException =>
                    new ErrorDetails(
                        StatusCodes.Status401Unauthorized,
                        "Authentication failed",
                        "UNAUTHORIZED",
                        exception.Message),

                KeyNotFoundException =>
                    new ErrorDetails(
                        StatusCodes.Status404NotFound,
                        "Resource not found",
                        "NOT_FOUND",
                        exception.Message),

                NotSupportedException =>
                    new ErrorDetails(
                        StatusCodes.Status400BadRequest,
                        "Unsupported operation",
                        "NOT_SUPPORTED",
                        exception.Message),

                ArgumentException =>
                    new ErrorDetails(
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        "INVALID_REQUEST",
                        exception.Message),

                /*
                 * Used for business-state conflicts:
                 *
                 * - Booking already confirmed
                 * - Booking payment window expired
                 * - Pending payment already exists
                 * - Booking cannot currently be paid
                 */
                InvalidOperationException =>
                    new ErrorDetails(
                        StatusCodes.Status409Conflict,
                        "Operation conflict",
                        "OPERATION_CONFLICT",
                        exception.Message),

                _ =>
                    new ErrorDetails(
                        StatusCodes.Status500InternalServerError,
                        "Internal server error",
                        "INTERNAL_SERVER_ERROR",
                        "An unexpected error occurred.")

            };
        }

        private sealed record ErrorDetails(
            int StatusCode,
            string Title,
            string Code,
            string Detail);
    }
}