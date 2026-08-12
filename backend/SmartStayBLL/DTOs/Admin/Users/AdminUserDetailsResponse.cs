namespace SmartStayBLL
{
    public sealed class AdminUserDetailsResponse
    {
        public Guid UserId { get; set; }

        public string? Email { get; set; }

        public string? UserName { get; set; }

        public string? PhoneNumber { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? ProfileImageUrl { get; set; }

        public bool IsActive { get; set; }

        public bool IsProfileCompleted { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public IReadOnlyList<string> Roles { get; set; }
            = new List<string>();

        public int ActiveRefreshTokensCount { get; set; }

        public AdminUserHostProfileResponse? HostProfile { get; set; }

        public AdminUserBookingStatsResponse GuestBookingStats { get; set; }
            = new();

        public AdminUserPropertyStatsResponse HostPropertyStats { get; set; }
            = new();
    }

    public sealed class AdminUserHostProfileResponse
    {
        public Guid HostProfileId { get; set; }

        public string DisplayName { get; set; } =
            string.Empty;

        public string? ProfileImageUrl { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string? RejectionReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }
    }

    public sealed class AdminUserBookingStatsResponse
    {
        public int TotalBookings { get; set; }

        public int PendingBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int CancelledBookings { get; set; }

        public int CompletedBookings { get; set; }

        public int ExpiredBookings { get; set; }
    }

    public sealed class AdminUserPropertyStatsResponse
    {
        public int TotalProperties { get; set; }

        public int DraftProperties { get; set; }

        public int PendingProperties { get; set; }

        public int PublishedProperties { get; set; }

        public int RejectedProperties { get; set; }

        public int UnpublishedProperties { get; set; }
    }
}