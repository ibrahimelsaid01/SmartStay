using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminDashboardService
        : IAdminDashboardService
    {
        private readonly SmartStayDbContext _dbContext;

        public AdminDashboardService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        public async Task<AdminDashboardSummaryResponse>
            GetSummaryAsync(
                CancellationToken cancellationToken = default)
        {
            var totalUsers =
                await _dbContext.Users
                    .AsNoTracking()
                    .CountAsync(
                        cancellationToken);

            var activeUsers =
                await _dbContext.Users
                    .AsNoTracking()
                    .CountAsync(
                        user =>
                            user.IsActive,
                        cancellationToken);

            var inactiveUsers =
                totalUsers - activeUsers;

            var roleCounts =
                await GetRoleCountsAsync(
                    cancellationToken);

            var hostApplicationCounts =
                await GetHostApplicationCountsAsync(
                    cancellationToken);

            var propertyCounts =
                await GetPropertyCountsAsync(
                    cancellationToken);

            var bookingCounts =
                await GetBookingCountsAsync(
                    cancellationToken);

            var financials =
                await GetFinancialSummariesAsync(
                    cancellationToken);

            return new AdminDashboardSummaryResponse
            {
                GeneratedAt =
                    DateTimeOffset.UtcNow,

                TotalUsers =
                    totalUsers,

                ActiveUsers =
                    activeUsers,

                InactiveUsers =
                    inactiveUsers,

                TotalGuests =
                    GetRoleCount(
                        roleCounts,
                        RoleNames.User),

                TotalHosts =
                    GetRoleCount(
                        roleCounts,
                        RoleNames.Host),

                TotalAdmins =
                    GetRoleCount(
                        roleCounts,
                        RoleNames.Admin),

                TotalHostApplications =
                    hostApplicationCounts.Total,

                DraftHostApplications =
                    hostApplicationCounts.Draft,

                PendingHostApplications =
                    hostApplicationCounts.Pending,

                ApprovedHostApplications =
                    hostApplicationCounts.Approved,

                RejectedHostApplications =
                    hostApplicationCounts.Rejected,

                TotalProperties =
                    propertyCounts.Total,

                TotalListings =
                    propertyCounts.Total,

                DraftProperties =
                    propertyCounts.Draft,

                PendingPropertyVerifications =
                    propertyCounts.Pending,

                PublishedProperties =
                    propertyCounts.Published,

                RejectedProperties =
                    propertyCounts.Rejected,

                UnpublishedProperties =
                    propertyCounts.Unpublished,

                PendingVerifications =
                    hostApplicationCounts.Pending
                    +
                    propertyCounts.Pending,

                TotalBookings =
                    bookingCounts.Total,

                PendingBookings =
                    bookingCounts.Pending,

                ConfirmedBookings =
                    bookingCounts.Confirmed,

                CancelledBookings =
                    bookingCounts.Cancelled,

                CompletedBookings =
                    bookingCounts.Completed,

                ExpiredBookings =
                    bookingCounts.Expired,

                Financials =
                    financials
            };
        }

        // =====================================================
        // Users / roles
        // =====================================================

        private async Task<Dictionary<string, int>>
            GetRoleCountsAsync(
                CancellationToken cancellationToken)
        {
            return await (
                    from role in _dbContext.Roles.AsNoTracking()
                    join userRole in _dbContext.UserRoles.AsNoTracking()
                        on role.Id equals userRole.RoleId
                    where role.Name != null
                    group userRole by role.Name
                    into roleGroup
                    select new
                    {
                        RoleName =
                            roleGroup.Key!,

                        Count =
                            roleGroup.Count()
                    })
                .ToDictionaryAsync(
                    item =>
                        item.RoleName,

                    item =>
                        item.Count,

                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);
        }

        private static int GetRoleCount(
            IReadOnlyDictionary<string, int> roleCounts,
            string roleName)
        {
            return roleCounts.TryGetValue(
                    roleName,
                    out var count)
                ? count
                : 0;
        }

        // =====================================================
        // Host applications
        // =====================================================

        private async Task<HostApplicationCounts>
            GetHostApplicationCountsAsync(
                CancellationToken cancellationToken)
        {
            var groupedCounts =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .GroupBy(
                        host =>
                            host.Status)
                    .Select(
                        group =>
                            new
                            {
                                Status =
                                    group.Key,

                                Count =
                                    group.Count()
                            })
                    .ToListAsync(
                        cancellationToken);

            var result =
                new HostApplicationCounts();

            foreach (var item in groupedCounts)
            {
                result.Total +=
                    item.Count;

                switch (item.Status)
                {
                    case HostApplicationStatus.Draft:
                        result.Draft =
                            item.Count;
                        break;

                    case HostApplicationStatus.Pending:
                        result.Pending =
                            item.Count;
                        break;

                    case HostApplicationStatus.Approved:
                        result.Approved =
                            item.Count;
                        break;

                    case HostApplicationStatus.Rejected:
                        result.Rejected =
                            item.Count;
                        break;
                }
            }

            return result;
        }

        // =====================================================
        // Properties
        // =====================================================

        private async Task<PropertyCounts>
            GetPropertyCountsAsync(
                CancellationToken cancellationToken)
        {
            var groupedCounts =
                await _dbContext.Properties
                    .AsNoTracking()
                    .GroupBy(
                        property =>
                            property.Status)
                    .Select(
                        group =>
                            new
                            {
                                Status =
                                    group.Key,

                                Count =
                                    group.Count()
                            })
                    .ToListAsync(
                        cancellationToken);

            var result =
                new PropertyCounts();

            foreach (var item in groupedCounts)
            {
                result.Total +=
                    item.Count;

                switch (item.Status)
                {
                    case PropertyStatus.Draft:
                        result.Draft =
                            item.Count;
                        break;

                    case PropertyStatus.Pending:
                        result.Pending =
                            item.Count;
                        break;

                    case PropertyStatus.Published:
                        result.Published =
                            item.Count;
                        break;

                    case PropertyStatus.Rejected:
                        result.Rejected =
                            item.Count;
                        break;

                    case PropertyStatus.Unpublished:
                        result.Unpublished =
                            item.Count;
                        break;
                }
            }

            return result;
        }

        // =====================================================
        // Bookings
        // =====================================================

        private async Task<BookingCounts>
            GetBookingCountsAsync(
                CancellationToken cancellationToken)
        {
            var groupedCounts =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .GroupBy(
                        booking =>
                            booking.Status)
                    .Select(
                        group =>
                            new
                            {
                                Status =
                                    group.Key,

                                Count =
                                    group.Count()
                            })
                    .ToListAsync(
                        cancellationToken);

            var result =
                new BookingCounts();

            foreach (var item in groupedCounts)
            {
                result.Total +=
                    item.Count;

                switch (item.Status)
                {
                    case BookingStatus.Pending:
                        result.Pending =
                            item.Count;
                        break;

                    case BookingStatus.Confirmed:
                        result.Confirmed =
                            item.Count;
                        break;

                    case BookingStatus.Cancelled:
                        result.Cancelled =
                            item.Count;
                        break;

                    case BookingStatus.Completed:
                        result.Completed =
                            item.Count;
                        break;

                    case BookingStatus.Expired:
                        result.Expired =
                            item.Count;
                        break;
                }
            }

            return result;
        }

        // =====================================================
        // Financials
        // =====================================================

        private async Task<
            IReadOnlyList<AdminDashboardFinancialSummaryResponse>>
            GetFinancialSummariesAsync(
                CancellationToken cancellationToken)
        {
            var financials =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .GroupBy(
                        payment =>
                            payment.Currency)
                    .Select(
                        group =>
                            new AdminDashboardFinancialSummaryResponse
                            {
                                Currency =
                                    group.Key,

                                TotalPaymentAttempts =
                                    group.Count(),

                                PendingPayments =
                                    group.Count(
                                        payment =>
                                            payment.Status ==
                                            PaymentStatus.Pending),

                                SuccessfulPayments =
                                    group.Count(
                                        payment =>
                                            payment.Status ==
                                            PaymentStatus.Succeeded
                                            ||
                                            payment.Status ==
                                            PaymentStatus.PartiallyRefunded
                                            ||
                                            payment.Status ==
                                            PaymentStatus.Refunded),

                                FailedPayments =
                                    group.Count(
                                        payment =>
                                            payment.Status ==
                                            PaymentStatus.Failed),

                                CancelledPayments =
                                    group.Count(
                                        payment =>
                                            payment.Status ==
                                            PaymentStatus.Cancelled),

                                PartiallyRefundedPayments =
                                    group.Count(
                                        payment =>
                                            payment.Status ==
                                            PaymentStatus.PartiallyRefunded),

                                FullyRefundedPayments =
                                    group.Count(
                                        payment =>
                                            payment.Status ==
                                            PaymentStatus.Refunded),

                                GrossVolume =
                                    group.Sum(
                                        payment =>
                                            payment.Status ==
                                                PaymentStatus.Succeeded
                                            ||
                                            payment.Status ==
                                                PaymentStatus.PartiallyRefunded
                                            ||
                                            payment.Status ==
                                                PaymentStatus.Refunded
                                                ? payment.Amount
                                                : 0m),

                                TotalRefundedAmount =
                                    group.Sum(
                                        payment =>
                                            payment.RefundedAmount)
                            })
                    .ToListAsync(
                        cancellationToken);

            var platformRevenueByCurrency =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .Where(
                        payment =>
                            payment.Status ==
                                PaymentStatus.Succeeded
                            ||
                            payment.Status ==
                                PaymentStatus.PartiallyRefunded
                            ||
                            payment.Status ==
                                PaymentStatus.Refunded)
                    .GroupBy(
                        payment =>
                            payment.Currency)
                    .Select(
                        group =>
                            new
                            {
                                Currency =
                                    group.Key,

                                PlatformRevenue =
                                    group.Sum(
                                        payment =>
                                            payment.Booking.ServiceFee)
                            })
                    .ToDictionaryAsync(
                        item =>
                            item.Currency,

                        item =>
                            item.PlatformRevenue,

                        StringComparer.OrdinalIgnoreCase,
                        cancellationToken);

            foreach (var item in financials)
            {
                item.PlatformRevenue =
                    platformRevenueByCurrency.TryGetValue(
                        item.Currency,
                        out var platformRevenue)
                        ? platformRevenue
                        : 0m;

                item.NetVolume =
                    item.GrossVolume
                    -
                    item.TotalRefundedAmount;

                item.SuccessRatePercentage =
                    CalculatePercentage(
                        item.SuccessfulPayments,
                        item.TotalPaymentAttempts);
            }

            if (financials.Count == 0)
            {
                financials.Add(
                    new AdminDashboardFinancialSummaryResponse
                    {
                        Currency =
                            "EGP",

                        TotalPaymentAttempts =
                            0,

                        PendingPayments =
                            0,

                        SuccessfulPayments =
                            0,

                        FailedPayments =
                            0,

                        CancelledPayments =
                            0,

                        PartiallyRefundedPayments =
                            0,

                        FullyRefundedPayments =
                            0,

                        GrossVolume =
                            0m,

                        PlatformRevenue =
                            0m,

                        TotalRefundedAmount =
                            0m,

                        NetVolume =
                            0m,

                        SuccessRatePercentage =
                            0m
                    });
            }

            return financials
                .OrderBy(
                    item =>
                        item.Currency,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static decimal CalculatePercentage(
            int value,
            int total)
        {
            if (total <= 0)
            {
                return 0m;
            }

            return Math.Round(
                value * 100m / total,
                2,
                MidpointRounding.AwayFromZero);
        }

        // =====================================================
        // Internal count models
        // =====================================================

        private sealed class HostApplicationCounts
        {
            public int Total { get; set; }

            public int Draft { get; set; }

            public int Pending { get; set; }

            public int Approved { get; set; }

            public int Rejected { get; set; }
        }

        private sealed class PropertyCounts
        {
            public int Total { get; set; }

            public int Draft { get; set; }

            public int Pending { get; set; }

            public int Published { get; set; }

            public int Rejected { get; set; }

            public int Unpublished { get; set; }
        }

        private sealed class BookingCounts
        {
            public int Total { get; set; }

            public int Pending { get; set; }

            public int Confirmed { get; set; }

            public int Cancelled { get; set; }

            public int Completed { get; set; }

            public int Expired { get; set; }
        }
    }
}