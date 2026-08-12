namespace SmartStayDAL
{
    public sealed class WishListItem
    {
        public Guid WishListId { get; set; }

        public Guid PropertyId { get; set; }

        public string? Note { get; set; }

        public DateTimeOffset AddedAt { get; set; }

        public WishList WishList { get; set; } =
            null!;

        public Property Property { get; set; } =
            null!;
    }
}