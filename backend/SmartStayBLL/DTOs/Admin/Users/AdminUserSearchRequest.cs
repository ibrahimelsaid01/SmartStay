namespace SmartStayBLL
{
    public sealed class AdminUserSearchRequest
    {
        public string? Search { get; set; }

        /*
         * Allowed values:
         * Admin, Host, User
         */
        public string? Role { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsProfileCompleted { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}