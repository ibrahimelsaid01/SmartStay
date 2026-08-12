namespace SmartStayBLL
{
    public sealed class AdminHostApplicationDetailsResponse
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } =
            string.Empty;

        public string UserFullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string PhoneNumber { get; set; } =
            string.Empty;

        public string Bio { get; set; } =
            string.Empty;

        public string Country { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string? ProfileImageUrl { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string? RejectionReason { get; set; }

        public bool HasProfileImage { get; set; }

        public bool HasIdentityDocument { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }
    }
}