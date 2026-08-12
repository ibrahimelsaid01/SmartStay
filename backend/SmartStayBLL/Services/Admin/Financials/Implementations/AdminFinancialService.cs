using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminFinancialService
        : IAdminFinancialService
    {
        private const int MaximumPageSize = 100;

        private readonly SmartStayDbContext _dbContext;

        public AdminFinancialService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        // =====================================================
        // Summary
        // =====================================================

        public async Task<AdminFinancialSummaryResponse> GetSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            var payments =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .Select(
                        payment =>
                            new PaymentSummaryProjection
                            {
                                Currency =
                                    payment.Currency,

                                Amount =
                                    payment.Amount,

                                ServiceFee =
                                    payment.Booking.ServiceFee,

                                RefundedAmount =
                                    payment.RefundedAmount,

                                Status =
                                    payment.Status
                            })
                    .ToListAsync(
                        cancellationToken);

            var refunds =
                await _dbContext.BookingPaymentRefunds
                    .AsNoTracking()
                    .Select(
                        refund =>
                            new RefundSummaryProjection
                            {
                                Currency =
                                    refund.Currency,

                                Amount =
                                    refund.Amount,

                                Status =
                                    refund.Status
                            })
                    .ToListAsync(
                        cancellationToken);

            var currencies =
                payments
                    .Select(
                        payment =>
                            NormalizeCurrencyForDisplay(
                                payment.Currency))
                    .Concat(
                        refunds.Select(
                            refund =>
                                NormalizeCurrencyForDisplay(
                                    refund.Currency)))
                    .Where(
                        currency =>
                            !string.IsNullOrWhiteSpace(
                                currency))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        currency =>
                            currency,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (currencies.Count == 0)
            {
                currencies.Add(
                    "EGP");
            }

            var summaries =
                currencies
                    .Select(
                        currency =>
                            BuildCurrencySummary(
                                currency,
                                payments,
                                refunds))
                    .ToList();

            return new AdminFinancialSummaryResponse
            {
                GeneratedAt =
                    DateTimeOffset.UtcNow,

                Currencies =
                    summaries
            };
        }

        private static AdminFinancialCurrencySummaryResponse
            BuildCurrencySummary(
                string currency,
                IReadOnlyList<PaymentSummaryProjection> payments,
                IReadOnlyList<RefundSummaryProjection> refunds)
        {
            var currencyPayments =
                payments
                    .Where(
                        payment =>
                            string.Equals(
                                NormalizeCurrencyForDisplay(
                                    payment.Currency),
                                currency,
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var currencyRefunds =
                refunds
                    .Where(
                        refund =>
                            string.Equals(
                                NormalizeCurrencyForDisplay(
                                    refund.Currency),
                                currency,
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var successfulPayments =
                currencyPayments
                    .Where(
                        payment =>
                            IsFinanciallySuccessfulPayment(
                                payment.Status))
                    .ToList();

            var successfulRefunds =
                currencyRefunds
                    .Where(
                        refund =>
                            refund.Status ==
                            PaymentRefundStatus.Succeeded)
                    .ToList();

            var grossVolume =
                successfulPayments.Sum(
                    payment =>
                        payment.Amount);

            var platformRevenue =
                successfulPayments.Sum(
                    payment =>
                        payment.ServiceFee);

            var totalRefundedAmount =
                successfulRefunds.Sum(
                    refund =>
                        refund.Amount);

            return new AdminFinancialCurrencySummaryResponse
            {
                Currency =
                    currency,

                TotalPaymentAttempts =
                    currencyPayments.Count,

                PendingPayments =
                    currencyPayments.Count(
                        payment =>
                            payment.Status ==
                            PaymentStatus.Pending),

                SuccessfulPayments =
                    successfulPayments.Count,

                FailedPayments =
                    currencyPayments.Count(
                        payment =>
                            payment.Status ==
                            PaymentStatus.Failed),

                CancelledPayments =
                    currencyPayments.Count(
                        payment =>
                            payment.Status ==
                            PaymentStatus.Cancelled),

                PartiallyRefundedPayments =
                    currencyPayments.Count(
                        payment =>
                            payment.Status ==
                            PaymentStatus.PartiallyRefunded),

                FullyRefundedPayments =
                    currencyPayments.Count(
                        payment =>
                            payment.Status ==
                            PaymentStatus.Refunded),

                GrossVolume =
                    RoundMoney(
                        grossVolume),

                PlatformRevenue =
                    RoundMoney(
                        platformRevenue),

                TotalRefundedAmount =
                    RoundMoney(
                        totalRefundedAmount),

                NetVolume =
                    RoundMoney(
                        grossVolume
                        -
                        totalRefundedAmount),

                TotalRefundRequests =
                    currencyRefunds.Count,

                PendingRefundRequests =
                    currencyRefunds.Count(
                        refund =>
                            refund.Status ==
                            PaymentRefundStatus.Pending
                            ||
                            refund.Status ==
                            PaymentRefundStatus.RequiresAction),

                SuccessfulRefundRequests =
                    successfulRefunds.Count,

                FailedRefundRequests =
                    currencyRefunds.Count(
                        refund =>
                            refund.Status ==
                            PaymentRefundStatus.Failed
                            ||
                            refund.Status ==
                            PaymentRefundStatus.Cancelled),

                SuccessRatePercentage =
                    CalculatePercentage(
                        successfulPayments.Count,
                        currencyPayments.Count),

                PendingPayoutRequests =
                    0,

                PendingPayoutAmount =
                    0m
            };
        }

        // =====================================================
        // Transactions
        // =====================================================

        public async Task<AdminFinancialTransactionsResponse> GetTransactionsAsync(
            AdminFinancialTransactionSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var page =
                NormalizePage(
                    request.Page);

            var pageSize =
                NormalizePageSize(
                    request.PageSize);

            var type =
                NormalizeTransactionType(
                    request.Type);

            var transactions =
                new List<AdminFinancialTransactionItemResponse>();

            if (type is "all" or "payment")
            {
                var payments =
                    await GetPaymentTransactionsAsync(
                        request,
                        cancellationToken);

                transactions.AddRange(
                    payments);
            }

            if (type is "all" or "refund")
            {
                var refunds =
                    await GetRefundTransactionsAsync(
                        request,
                        cancellationToken);

                transactions.AddRange(
                    refunds);
            }

            transactions =
                ApplyInMemorySearchFilter(
                        transactions,
                        request.Search)
                    .OrderByDescending(
                        transaction =>
                            transaction.CreatedAt)
                    .ThenBy(
                        transaction =>
                            transaction.ReferenceCode,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            var totalCount =
                transactions.Count;

            var pagedItems =
                transactions
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(
                        pageSize)
                    .ToList();

            return new AdminFinancialTransactionsResponse
            {
                Page =
                    page,

                PageSize =
                    pageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    CalculateTotalPages(
                        totalCount,
                        pageSize),

                Items =
                    pagedItems
            };
        }

        private async Task<IReadOnlyList<AdminFinancialTransactionItemResponse>>
            GetPaymentTransactionsAsync(
                AdminFinancialTransactionSearchRequest request,
                CancellationToken cancellationToken)
        {
            var query =
                _dbContext.BookingPayments
                    .AsNoTracking()
                    .AsQueryable();

            query =
                ApplyPaymentFilters(
                    query,
                    request);

            var rows =
                await query
                    .Select(
                        payment =>
                            new PaymentTransactionProjection
                            {
                                PaymentId =
                                    payment.Id,

                                BookingId =
                                    payment.BookingId,

                                Provider =
                                    payment.Provider,

                                ProviderPaymentId =
                                    payment.ProviderPaymentId,

                                ProviderReference =
                                    payment.ProviderReference,

                                Amount =
                                    payment.Amount,

                                Currency =
                                    payment.Currency,

                                PlatformFee =
                                    payment.Booking.ServiceFee,

                                RefundedAmount =
                                    payment.RefundedAmount,

                                Status =
                                    payment.Status,

                                FailureReason =
                                    payment.FailureMessage
                                    ??
                                    payment.FailureCode,

                                CreatedAt =
                                    payment.CreatedAt,

                                SucceededAt =
                                    payment.SucceededAt,

                                FailedAt =
                                    payment.FailedAt,

                                CancelledAt =
                                    payment.CancelledAt,

                                RefundedAt =
                                    payment.RefundedAt,

                                UserId =
                                    payment.Booking.GuestUserId,

                                UserFirstName =
                                    payment.Booking.GuestUser.FirstName,

                                UserLastName =
                                    payment.Booking.GuestUser.LastName,

                                UserEmail =
                                    payment.Booking.GuestUser.Email,

                                PropertyTitle =
                                    payment.Booking.Property.Title
                            })
                    .ToListAsync(
                        cancellationToken);

            return rows
                .Select(
                    payment =>
                    {
                        var completedAt =
                            ResolvePaymentCompletedAt(
                                payment);

                        return new AdminFinancialTransactionItemResponse
                        {
                            TransactionId =
                                payment.PaymentId,

                            ReferenceCode =
                                BuildReferenceCode(
                                    "TXN",
                                    payment.PaymentId),

                            Type =
                                "BookingPayment",

                            Direction =
                                "Incoming",

                            Provider =
                                payment.Provider,

                            ProviderTransactionId =
                                payment.ProviderPaymentId
                                ??
                                payment.ProviderReference,

                            BookingId =
                                payment.BookingId,

                            PaymentId =
                                payment.PaymentId,

                            RefundId =
                                null,

                            UserId =
                                payment.UserId,

                            UserName =
                                BuildFullName(
                                    payment.UserFirstName,
                                    payment.UserLastName,
                                    payment.UserEmail),

                            UserEmail =
                                payment.UserEmail,

                            PropertyTitle =
                                payment.PropertyTitle,

                            Currency =
                                NormalizeCurrencyForDisplay(
                                    payment.Currency),

                            Amount =
                                RoundMoney(
                                    payment.Amount),

                            SignedAmount =
                                RoundMoney(
                                    payment.Amount),

                            PlatformFee =
                                RoundMoney(
                                    payment.PlatformFee),

                            RefundedAmount =
                                RoundMoney(
                                    payment.RefundedAmount),

                            NetAmount =
                                RoundMoney(
                                    payment.Amount
                                    -
                                    payment.RefundedAmount),

                            Status =
                                payment.Status.ToString(),

                            FailureReason =
                                payment.FailureReason,

                            CreatedAt =
                                payment.CreatedAt,

                            CompletedAt =
                                completedAt
                        };
                    })
                .ToList();
        }

        private async Task<IReadOnlyList<AdminFinancialTransactionItemResponse>>
            GetRefundTransactionsAsync(
                AdminFinancialTransactionSearchRequest request,
                CancellationToken cancellationToken)
        {
            var query =
                _dbContext.BookingPaymentRefunds
                    .AsNoTracking()
                    .AsQueryable();

            query =
                ApplyRefundFilters(
                    query,
                    request);

            var rows =
                await query
                    .Select(
                        refund =>
                            new RefundTransactionProjection
                            {
                                RefundId =
                                    refund.Id,

                                PaymentId =
                                    refund.BookingPaymentId,

                                BookingId =
                                    refund.BookingPayment.BookingId,

                                Provider =
                                    refund.Provider,

                                ProviderRefundId =
                                    refund.ProviderRefundId,

                                Amount =
                                    refund.Amount,

                                Currency =
                                    refund.Currency,

                                Status =
                                    refund.Status,

                                FailureReason =
                                    refund.FailureReason,

                                CreatedAt =
                                    refund.CreatedAt,

                                SucceededAt =
                                    refund.SucceededAt,

                                FailedAt =
                                    refund.FailedAt,

                                CancelledAt =
                                    refund.CancelledAt,

                                UserId =
                                    refund.BookingPayment.Booking.GuestUserId,

                                UserFirstName =
                                    refund.BookingPayment.Booking.GuestUser.FirstName,

                                UserLastName =
                                    refund.BookingPayment.Booking.GuestUser.LastName,

                                UserEmail =
                                    refund.BookingPayment.Booking.GuestUser.Email,

                                PropertyTitle =
                                    refund.BookingPayment.Booking.Property.Title
                            })
                    .ToListAsync(
                        cancellationToken);

            return rows
                .Select(
                    refund =>
                    {
                        var completedAt =
                            ResolveRefundCompletedAt(
                                refund);

                        return new AdminFinancialTransactionItemResponse
                        {
                            TransactionId =
                                refund.RefundId,

                            ReferenceCode =
                                BuildReferenceCode(
                                    "REF",
                                    refund.RefundId),

                            Type =
                                "Refund",

                            Direction =
                                "Outgoing",

                            Provider =
                                refund.Provider,

                            ProviderTransactionId =
                                refund.ProviderRefundId,

                            BookingId =
                                refund.BookingId,

                            PaymentId =
                                refund.PaymentId,

                            RefundId =
                                refund.RefundId,

                            UserId =
                                refund.UserId,

                            UserName =
                                BuildFullName(
                                    refund.UserFirstName,
                                    refund.UserLastName,
                                    refund.UserEmail),

                            UserEmail =
                                refund.UserEmail,

                            PropertyTitle =
                                refund.PropertyTitle,

                            Currency =
                                NormalizeCurrencyForDisplay(
                                    refund.Currency),

                            Amount =
                                RoundMoney(
                                    refund.Amount),

                            SignedAmount =
                                RoundMoney(
                                    -refund.Amount),

                            PlatformFee =
                                0m,

                            RefundedAmount =
                                RoundMoney(
                                    refund.Amount),

                            NetAmount =
                                RoundMoney(
                                    -refund.Amount),

                            Status =
                                refund.Status.ToString(),

                            FailureReason =
                                refund.FailureReason,

                            CreatedAt =
                                refund.CreatedAt,

                            CompletedAt =
                                completedAt
                        };
                    })
                .ToList();
        }

        // =====================================================
        // Filters
        // =====================================================

        private static IQueryable<BookingPayment> ApplyPaymentFilters(
            IQueryable<BookingPayment> query,
            AdminFinancialTransactionSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(
                    request.Currency))
            {
                var currency =
                    NormalizeCurrencyForDisplay(
                        request.Currency);

                query =
                    query.Where(
                        payment =>
                            payment.Currency == currency);
            }

            if (request.FromDate.HasValue)
            {
                query =
                    query.Where(
                        payment =>
                            payment.CreatedAt >=
                            request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query =
                    query.Where(
                        payment =>
                            payment.CreatedAt <=
                            request.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    request.Status)
                &&
                TryParsePaymentStatus(
                    request.Status,
                    out var paymentStatus))
            {
                query =
                    query.Where(
                        payment =>
                            payment.Status ==
                            paymentStatus);
            }

            return query;
        }

        private static IQueryable<BookingPaymentRefund> ApplyRefundFilters(
            IQueryable<BookingPaymentRefund> query,
            AdminFinancialTransactionSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(
                    request.Currency))
            {
                var currency =
                    NormalizeCurrencyForDisplay(
                        request.Currency);

                query =
                    query.Where(
                        refund =>
                            refund.Currency == currency);
            }

            if (request.FromDate.HasValue)
            {
                query =
                    query.Where(
                        refund =>
                            refund.CreatedAt >=
                            request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query =
                    query.Where(
                        refund =>
                            refund.CreatedAt <=
                            request.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    request.Status)
                &&
                TryParseRefundStatus(
                    request.Status,
                    out var refundStatus))
            {
                query =
                    query.Where(
                        refund =>
                            refund.Status ==
                            refundStatus);
            }

            return query;
        }

        private static IReadOnlyList<AdminFinancialTransactionItemResponse>
            ApplyInMemorySearchFilter(
                IReadOnlyList<AdminFinancialTransactionItemResponse> transactions,
                string? search)
        {
            if (string.IsNullOrWhiteSpace(
                    search))
            {
                return transactions;
            }

            var normalizedSearch =
                search.Trim();

            return transactions
                .Where(
                    transaction =>
                        ContainsIgnoreCase(
                            transaction.ReferenceCode,
                            normalizedSearch)
                        ||
                        ContainsIgnoreCase(
                            transaction.ProviderTransactionId,
                            normalizedSearch)
                        ||
                        ContainsIgnoreCase(
                            transaction.UserName,
                            normalizedSearch)
                        ||
                        ContainsIgnoreCase(
                            transaction.UserEmail,
                            normalizedSearch)
                        ||
                        ContainsIgnoreCase(
                            transaction.PropertyTitle,
                            normalizedSearch)
                        ||
                        ContainsIgnoreCase(
                            transaction.Status,
                            normalizedSearch)
                        ||
                        transaction.BookingId.HasValue
                        &&
                        ContainsIgnoreCase(
                            transaction.BookingId.Value.ToString(),
                            normalizedSearch)
                        ||
                        transaction.PaymentId.HasValue
                        &&
                        ContainsIgnoreCase(
                            transaction.PaymentId.Value.ToString(),
                            normalizedSearch)
                        ||
                        transaction.RefundId.HasValue
                        &&
                        ContainsIgnoreCase(
                            transaction.RefundId.Value.ToString(),
                            normalizedSearch))
                .ToList();
        }

        // =====================================================
        // Helpers
        // =====================================================

        private static bool IsFinanciallySuccessfulPayment(
            PaymentStatus status)
        {
            return status is
                PaymentStatus.Succeeded
                or PaymentStatus.PartiallyRefunded
                or PaymentStatus.Refunded;
        }

        private static DateTimeOffset? ResolvePaymentCompletedAt(
            PaymentTransactionProjection payment)
        {
            return payment.Status switch
            {
                PaymentStatus.Succeeded =>
                    payment.SucceededAt,

                PaymentStatus.Failed =>
                    payment.FailedAt,

                PaymentStatus.Cancelled =>
                    payment.CancelledAt,

                PaymentStatus.PartiallyRefunded
                    or PaymentStatus.Refunded =>
                    payment.RefundedAt
                    ??
                    payment.SucceededAt,

                _ =>
                    null
            };
        }

        private static DateTimeOffset? ResolveRefundCompletedAt(
            RefundTransactionProjection refund)
        {
            return refund.Status switch
            {
                PaymentRefundStatus.Succeeded =>
                    refund.SucceededAt,

                PaymentRefundStatus.Failed =>
                    refund.FailedAt,

                PaymentRefundStatus.Cancelled =>
                    refund.CancelledAt,

                _ =>
                    null
            };
        }

        private static bool TryParsePaymentStatus(
            string value,
            out PaymentStatus status)
        {
            return Enum.TryParse(
                    value,
                    ignoreCase: true,
                    out status)
                &&
                Enum.IsDefined(
                    status);
        }

        private static bool TryParseRefundStatus(
            string value,
            out PaymentRefundStatus status)
        {
            return Enum.TryParse(
                    value,
                    ignoreCase: true,
                    out status)
                &&
                Enum.IsDefined(
                    status);
        }

        private static string NormalizeTransactionType(
            string? type)
        {
            if (string.IsNullOrWhiteSpace(
                    type))
            {
                return "all";
            }

            var normalizedType =
                type.Trim()
                    .ToLowerInvariant();

            return normalizedType switch
            {
                "all" =>
                    "all",

                "payment" or "payments" or "bookingpayment" or "bookingpayments" =>
                    "payment",

                "refund" or "refunds" =>
                    "refund",

                _ =>
                    throw new ArgumentException(
                        "The transaction type is invalid. Allowed values are all, payment, and refund.")
            };
        }

        private static string NormalizeCurrencyForDisplay(
            string? currency)
        {
            if (string.IsNullOrWhiteSpace(
                    currency))
            {
                return string.Empty;
            }

            return currency.Trim()
                .ToUpperInvariant();
        }

        private static string BuildReferenceCode(
            string prefix,
            Guid id)
        {
            var normalizedId =
                id.ToString("N");

            return
                $"{prefix}-{normalizedId.Substring(0, 4).ToUpperInvariant()}";
        }

        private static bool ContainsIgnoreCase(
            string? value,
            string search)
        {
            return !string.IsNullOrWhiteSpace(
                    value)
                &&
                value.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase);
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

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static int NormalizePage(
            int page)
        {
            return page <= 0
                ? 1
                : page;
        }

        private static int NormalizePageSize(
            int pageSize)
        {
            if (pageSize <= 0)
            {
                return 20;
            }

            return pageSize > MaximumPageSize
                ? MaximumPageSize
                : pageSize;
        }

        private static int CalculateTotalPages(
            int totalCount,
            int pageSize)
        {
            if (totalCount <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(
                totalCount / (double)pageSize);
        }

        private static string BuildFullName(
            string? firstName,
            string? lastName,
            string? fallback)
        {
            var fullName =
                string.Join(
                    " ",
                    new[]
                    {
                        firstName,
                        lastName
                    }
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(
                        value =>
                            value!.Trim()));

            if (!string.IsNullOrWhiteSpace(
                    fullName))
            {
                return fullName;
            }

            return fallback
                ??
                "Unknown User";
        }

        // =====================================================
        // Internal projections
        // =====================================================

        private sealed class PaymentSummaryProjection
        {
            public string Currency { get; init; } =
                string.Empty;

            public decimal Amount { get; init; }

            public decimal ServiceFee { get; init; }

            public decimal RefundedAmount { get; init; }

            public PaymentStatus Status { get; init; }
        }

        private sealed class RefundSummaryProjection
        {
            public string Currency { get; init; } =
                string.Empty;

            public decimal Amount { get; init; }

            public PaymentRefundStatus Status { get; init; }
        }

        private sealed class PaymentTransactionProjection
        {
            public Guid PaymentId { get; init; }

            public Guid BookingId { get; init; }

            public string Provider { get; init; } =
                string.Empty;

            public string? ProviderPaymentId { get; init; }

            public string? ProviderReference { get; init; }

            public decimal Amount { get; init; }

            public string Currency { get; init; } =
                string.Empty;

            public decimal PlatformFee { get; init; }

            public decimal RefundedAmount { get; init; }

            public PaymentStatus Status { get; init; }

            public string? FailureReason { get; init; }

            public DateTimeOffset CreatedAt { get; init; }

            public DateTimeOffset? SucceededAt { get; init; }

            public DateTimeOffset? FailedAt { get; init; }

            public DateTimeOffset? CancelledAt { get; init; }

            public DateTimeOffset? RefundedAt { get; init; }

            public Guid UserId { get; init; }

            public string? UserFirstName { get; init; }

            public string? UserLastName { get; init; }

            public string? UserEmail { get; init; }

            public string? PropertyTitle { get; init; }
        }

        private sealed class RefundTransactionProjection
        {
            public Guid RefundId { get; init; }

            public Guid PaymentId { get; init; }

            public Guid BookingId { get; init; }

            public string Provider { get; init; } =
                string.Empty;

            public string? ProviderRefundId { get; init; }

            public decimal Amount { get; init; }

            public string Currency { get; init; } =
                string.Empty;

            public PaymentRefundStatus Status { get; init; }

            public string? FailureReason { get; init; }

            public DateTimeOffset CreatedAt { get; init; }

            public DateTimeOffset? SucceededAt { get; init; }

            public DateTimeOffset? FailedAt { get; init; }

            public DateTimeOffset? CancelledAt { get; init; }

            public Guid UserId { get; init; }

            public string? UserFirstName { get; init; }

            public string? UserLastName { get; init; }

            public string? UserEmail { get; init; }

            public string? PropertyTitle { get; init; }
        }
    }
}