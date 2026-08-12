using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/webhooks/stripe")]
    [AllowAnonymous]
    public sealed class StripeWebhooksController
        : ControllerBase
    {
        private readonly IStripeWebhookService
            _stripeWebhookService;

        public StripeWebhooksController(
            IStripeWebhookService stripeWebhookService)
        {
            ArgumentNullException.ThrowIfNull(
                stripeWebhookService);

            _stripeWebhookService =
                stripeWebhookService;
        }

        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        public async Task<IActionResult>
            HandleAsync(
                CancellationToken cancellationToken)
        {
            /*
             * Do not use [FromBody] here.
             *
             * Stripe signature validation requires the
             * exact, unmodified raw JSON payload.
             */
            using var reader =
                new StreamReader(
                    Request.Body);

            var payload =
                await reader.ReadToEndAsync(
                    cancellationToken);

            var signatureHeader =
                Request.Headers[
                    "Stripe-Signature"]
                    .ToString();

            await _stripeWebhookService
                .ProcessAsync(
                    payload,
                    signatureHeader,
                    cancellationToken);

            return Ok(
                new
                {
                    received =
                        true
                });
        }
    }
}