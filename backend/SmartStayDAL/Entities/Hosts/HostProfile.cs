namespace SmartStayDAL
{
    public sealed class HostProfile
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string DisplayName { get; set; } =
            string.Empty;

        public string Bio { get; set; } =
            string.Empty;

        public string Country { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string? ProfileImageUrl { get; set; }

        public string? ProfileImagePublicId { get; set; }

        public HostApplicationStatus Status { get; set; } =
            HostApplicationStatus.Draft;

        public string? RejectionReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public HostIdentityDocument? IdentityDocument { get; set; }
        public ApplicationUser User { get; set; } =
            null!;

        public ICollection<Property> Properties
        {
            get;
            set;
        } = new List<Property>();


    }
}