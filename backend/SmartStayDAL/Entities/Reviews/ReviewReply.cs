namespace SmartStayDAL
{
    public sealed class ReviewReply
    {
        public Guid Id { get; set; }

        public Guid ReviewId { get; set; }

        public Guid HostProfileId { get; set; }

        public string Content { get; set; } =
            string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Review Review { get; set; } =
            null!;

        public HostProfile HostProfile { get; set; } =
            null!;
    }
}