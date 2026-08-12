namespace SmartStayBLL
{
    public sealed class ReviewHelpfulResponse
    {
        public Guid ReviewId { get; set; }

        public int HelpfulCount { get; set; }

        public bool IsHelpfulByCurrentUser { get; set; }
    }
}