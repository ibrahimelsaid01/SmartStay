namespace SmartStayBLL
{
    public sealed class PublicReviewResponse
    {
        public Guid Id { get; set; }

        public int Rating { get; set; }

        public string? PositiveComment { get; set; }

        public string? NegativeComment { get; set; }

        public int HelpfulCount { get; set; }

        /*
         * False for anonymous visitors.
         */
        public bool IsHelpfulByCurrentUser { get; set; }

        public ReviewAuthorResponse Author
        { get; set; } = new();

        public ReviewReplyResponse? Reply { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }
    }
}