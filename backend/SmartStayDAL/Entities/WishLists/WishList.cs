namespace SmartStayDAL
{
    public sealed class WishList
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public ApplicationUser User { get; set; } =
            null!;

        public ICollection<WishListItem> Items { get; set; } =
            new List<WishListItem>();
    }
}