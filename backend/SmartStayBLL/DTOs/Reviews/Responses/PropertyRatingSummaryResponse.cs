namespace SmartStayBLL
{
    public sealed class PropertyRatingSummaryResponse
    {
        public Guid PropertyId { get; set; }

        public decimal AverageRating { get; set; }

        public int ReviewsCount { get; set; }

        /*
         * JSON result:
         *
         * {
         *   "1": 2,
         *   "2": 1,
         *   "3": 5,
         *   "4": 20,
         *   "5": 80
         * }
         */
        public IReadOnlyDictionary<int, int> Distribution
        { get; set; } =
            new Dictionary<int, int>();
    }
}