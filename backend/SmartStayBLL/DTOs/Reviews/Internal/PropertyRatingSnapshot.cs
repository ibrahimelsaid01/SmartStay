namespace SmartStayBLL
{
    public sealed class PropertyRatingSnapshot
    {
        public Guid PropertyId { get; set; }

        public decimal AverageRating { get; set; }

        public int ReviewsCount { get; set; }
    }
}