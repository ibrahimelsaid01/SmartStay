namespace SmartStayBLL
{
    public sealed class UserProfileResponse
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; }
            = string.Empty;

        public string LastName { get; set; }
            = string.Empty;

        public string Email { get; set; }
            = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? ProfileImageUrl { get; set; }

        public string? Gender { get; set; }

        public DateOnly? Birthday { get; set; }

        public string? Country { get; set; }

        public string? Address { get; set; }

        public string? ZipCode { get; set; }

        public bool IsProfileCompleted { get; set; }

        public IList<string> Roles { get; set; }
            = new List<string>();

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}