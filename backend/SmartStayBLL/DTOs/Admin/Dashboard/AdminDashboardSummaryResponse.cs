namespace SmartStayBLL
{
    public sealed class AdminDashboardSummaryResponse
    {
        public DateTimeOffset GeneratedAt { get; set; }

        // =====================================================
        // Users
        // =====================================================

        public int TotalUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int InactiveUsers { get; set; }

        public int TotalGuests { get; set; }

        public int TotalHosts { get; set; }

        public int TotalAdmins { get; set; }

        // =====================================================
        // Host applications
        // =====================================================

        public int TotalHostApplications { get; set; }

        public int DraftHostApplications { get; set; }

        public int PendingHostApplications { get; set; }

        public int ApprovedHostApplications { get; set; }

        public int RejectedHostApplications { get; set; }

        // =====================================================
        // Properties / listings
        // =====================================================

        public int TotalProperties { get; set; }

        /*
         * Alias for the UI because the Figma dashboard
         * uses "Listings" wording.
         */
        public int TotalListings { get; set; }

        public int DraftProperties { get; set; }

        public int PendingPropertyVerifications { get; set; }

        public int PublishedProperties { get; set; }

        public int RejectedProperties { get; set; }

        public int UnpublishedProperties { get; set; }

        /*
         * Combined number used by the dashboard card.
         */
        public int PendingVerifications { get; set; }

        // =====================================================
        // Bookings
        // =====================================================

        public int TotalBookings { get; set; }

        public int PendingBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int CancelledBookings { get; set; }

        public int CompletedBookings { get; set; }

        public int ExpiredBookings { get; set; }

        // =====================================================
        // Payments / financials
        // =====================================================

        public IReadOnlyList<
            AdminDashboardFinancialSummaryResponse>
            Financials
        { get; set; }
            = new List<AdminDashboardFinancialSummaryResponse>();
    }

    public sealed class AdminDashboardFinancialSummaryResponse
    {
        public string Currency { get; set; } =
            string.Empty;

        public int TotalPaymentAttempts { get; set; }

        public int PendingPayments { get; set; }

        public int SuccessfulPayments { get; set; }

        public int FailedPayments { get; set; }

        public int CancelledPayments { get; set; }

        public int PartiallyRefundedPayments { get; set; }

        public int FullyRefundedPayments { get; set; }

        /*
         * Gross paid volume from successful financial
         * payment states before subtracting refunds.
         */
        public decimal GrossVolume { get; set; }

        /*
         * Service fee revenue collected by the platform.
         */
        public decimal PlatformRevenue { get; set; }

        public decimal TotalRefundedAmount { get; set; }

        public decimal NetVolume { get; set; }

        public decimal SuccessRatePercentage { get; set; }
    }
}