namespace SmartStayDAL
{
    public sealed class HostIdentityDocument
    {
        public Guid Id { get; set; }

        public Guid HostProfileId { get; set; }

        public string FrontPublicId { get; set; } =
            string.Empty;

        public string FrontFormat { get; set; } =
            string.Empty;

        public string BackPublicId { get; set; } =
            string.Empty;

        public string BackFormat { get; set; } =
            string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public HostProfile HostProfile { get; set; } =
            null!;
    }
}