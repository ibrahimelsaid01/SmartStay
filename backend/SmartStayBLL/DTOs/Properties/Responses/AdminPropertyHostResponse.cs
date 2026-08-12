namespace SmartStayBLL
{
    public sealed class AdminPropertyHostResponse
    {
        public Guid UserId { get; set; }

        public Guid HostProfileId { get; set; }

        public string FirstName { get; set; } =
            string.Empty;

        public string LastName { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public string HostStatus { get; set; } =
            string.Empty;
    }
}