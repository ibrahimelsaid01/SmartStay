namespace SmartStayBLL
{
    public sealed class ReviewAuthorResponse
    {
        public Guid UserId { get; set; }

        /*
         * Example:
         * Ahmed A.
         *
         * We do not expose the guest's full last name
         * publicly.
         */
        public string DisplayName { get; set; } =
            string.Empty;

        public string? ProfileImageUrl { get; set; }
    }
}