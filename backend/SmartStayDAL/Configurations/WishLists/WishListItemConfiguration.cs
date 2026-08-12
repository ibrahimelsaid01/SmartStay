using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class WishListItemConfiguration
        : IEntityTypeConfiguration<WishListItem>
    {
        public void Configure(
            EntityTypeBuilder<WishListItem> builder)
        {
            builder.ToTable("WishListItems");

            builder.HasKey(item =>
                new
                {
                    item.WishListId,
                    item.PropertyId
                });

            builder.Property(item =>
                    item.Note)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(item =>
                    item.AddedAt)
                .IsRequired();

            builder.HasOne(item =>
                    item.WishList)
                .WithMany(wishList =>
                    wishList.Items)
                .HasForeignKey(item =>
                    item.WishListId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasOne(item =>
                    item.Property)
                .WithMany()
                .HasForeignKey(item =>
                    item.PropertyId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(item =>
                item.PropertyId);

            builder.HasIndex(item =>
                item.AddedAt);
        }
    }
}