namespace SmartStayBLL
{
    public sealed class WishListSummaryResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int ItemsCount { get; set; }

        /*
         * أول أربع صور لعمل Preview للكارت
         * الخاص بالـWishlist.
         */
        public IReadOnlyList<string> PreviewImageUrls
        { get; set; } =
            Array.Empty<string>();

        /*
         * تستخدم عندما يضغط المستخدم على Heart
         * لعقار معين.
         */
        public bool ContainsProperty { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}