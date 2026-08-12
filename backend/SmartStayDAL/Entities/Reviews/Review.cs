namespace SmartStayDAL
{
    public sealed class Review
    {
        public Guid Id { get; set; }

        /*
         * Every booking can have only one review.
         */
        public Guid BookingId { get; set; }

        /*
         * Stored directly for efficient property queries.
         * The service must ensure that this matches
         * Booking.PropertyId.
         */
        public Guid PropertyId { get; set; }

        /*
         * The guest who created the review.
         * The service must ensure that this matches
         * Booking.GuestUserId.
         */
        public Guid UserId { get; set; }

        /*
         * Rating must be between 1 and 5.
         */
        public int Rating { get; set; }

        public string? PositiveComment { get; set; }

        public string? NegativeComment { get; set; }

        public ReviewStatus Status { get; set; } =
            ReviewStatus.Pending;

        public string? RejectionReason { get; set; }

        /*
         * The admin who approved or rejected the review.
         */
        public Guid? ModeratedByUserId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? ModeratedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        public DateTimeOffset? RejectedAt { get; set; }

        public Booking Booking { get; set; } =
            null!;

        public Property Property { get; set; } =
            null!;

        public ApplicationUser User { get; set; } =
            null!;

        public ApplicationUser? ModeratedByUser
        { get; set; }

        public ReviewReply? Reply { get; set; }

        public ICollection<ReviewHelpfulVote>
            HelpfulVotes
        { get; set; } =
                new List<ReviewHelpfulVote>();
    }
}