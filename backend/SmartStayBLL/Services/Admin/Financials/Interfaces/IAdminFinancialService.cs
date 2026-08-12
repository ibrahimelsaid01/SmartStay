namespace SmartStayBLL
{
    public interface IAdminFinancialService
    {
        Task<AdminFinancialSummaryResponse> GetSummaryAsync(
            CancellationToken cancellationToken = default);

        Task<AdminFinancialTransactionsResponse> GetTransactionsAsync(
            AdminFinancialTransactionSearchRequest request,
            CancellationToken cancellationToken = default);
    }
}