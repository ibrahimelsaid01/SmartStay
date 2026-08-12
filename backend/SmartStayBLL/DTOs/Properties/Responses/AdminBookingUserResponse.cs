namespace SmartStayBLL
{
    public sealed class AdminBookingUserResponse
    {
        public Guid UserId { get; set; }

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
    }
}