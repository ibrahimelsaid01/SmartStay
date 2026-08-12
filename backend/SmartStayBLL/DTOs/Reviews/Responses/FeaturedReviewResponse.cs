namespace SmartStayBLL
{
    public sealed class FeaturedReviewResponse
    {
        public Guid Id { get; set; }

        public int Rating { get; set; }

        /*
         * The single review text displayed on the Home page.
         *
         * The service prefers the positive comment when it
         * exists and otherwise uses the negative comment.
         */
        public string Comment { get; set; } =
            string.Empty;

        public ReviewAuthorResponse Author
        { get; set; } = new();

        public ReviewPropertyResponse Property
        { get; set; } = new();

        /*
         * Featured reviews are always Posted reviews.
         * The service falls back to CreatedAt only if an older
         * database row does not contain PublishedAt.
         */
        public DateTimeOffset PublishedAt { get; set; }
    }
}