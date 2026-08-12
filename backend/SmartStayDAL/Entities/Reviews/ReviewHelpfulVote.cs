namespace SmartStayDAL
{
    public sealed class ReviewHelpfulVote
    {
        public Guid ReviewId { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Review Review { get; set; } =
            null!;

        public ApplicationUser User { get; set; } =
            null!;
    }
}