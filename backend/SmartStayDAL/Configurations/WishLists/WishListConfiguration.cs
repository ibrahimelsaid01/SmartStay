using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class WishListConfiguration
        : IEntityTypeConfiguration<WishList>
    {
        public void Configure(
            EntityTypeBuilder<WishList> builder)
        {
            builder.ToTable("WishLists");

            builder.HasKey(wishList =>
                wishList.Id);

            builder.Property(wishList =>
                    wishList.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(wishList =>
                    wishList.CreatedAt)
                .IsRequired();

            builder.Property(wishList =>
                    wishList.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(wishList =>
                    wishList.User)
                .WithMany()
                .HasForeignKey(wishList =>
                    wishList.UserId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasIndex(wishList =>
                wishList.UserId);

            builder.HasIndex(wishList =>
                    new
                    {
                        wishList.UserId,
                        wishList.Name
                    })
                .IsUnique();
        }
    }
}