namespace SmartStayBLL
{
    public sealed class AdminUsersResponse
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public IReadOnlyList<AdminUserListItemResponse>
            Items
        { get; set; }
            = new List<AdminUserListItemResponse>();
    }

    public sealed class AdminUserListItemResponse
    {
        public Guid UserId { get; set; }

        public string? Email { get; set; }

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

        public bool IsHost { get; set; }

        public Guid? HostProfileId { get; set; }

        public string? HostStatus { get; set; }

        public int PropertiesCount { get; set; }

        public int GuestBookingsCount { get; set; }
    }
}