using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/admin/financials")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminFinancialsController
        : ControllerBase
    {
        private readonly IAdminFinancialService
            _adminFinancialService;

        public AdminFinancialsController(
            IAdminFinancialService adminFinancialService)
        {
            ArgumentNullException.ThrowIfNull(
                adminFinancialService);

            _adminFinancialService =
                adminFinancialService;
        }

        /*
         * GET:
         * /api/admin/financials/summary
         */
        [HttpGet("summary")]
        [ProducesResponseType(
            typeof(AdminFinancialSummaryResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminFinancialSummaryResponse>>
            GetSummaryAsync(
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminFinancialService
                    .GetSummaryAsync(
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * GET:
         * /api/admin/financials/transactions
         *
         * Examples:
         *
         * /api/admin/financials/transactions
         * /api/admin/financials/transactions?type=payment
         * /api/admin/financials/transactions?type=refund
         * /api/admin/financials/transactions?currency=EGP
         * /api/admin/financials/transactions?status=Succeeded
         */
        [HttpGet("transactions")]
        [ProducesResponseType(
            typeof(AdminFinancialTransactionsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminFinancialTransactionsResponse>>
            GetTransactionsAsync(
                [FromQuery] AdminFinancialTransactionSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _adminFinancialService
                    .GetTransactionsAsync(
                        request,
                        cancellationToken);

            return Ok(
                response);
        }
    }
}