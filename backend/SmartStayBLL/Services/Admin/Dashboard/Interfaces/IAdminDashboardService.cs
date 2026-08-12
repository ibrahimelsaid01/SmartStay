namespace SmartStayBLL
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardSummaryResponse>
            GetSummaryAsync(
                CancellationToken cancellationToken = default);
    }
}