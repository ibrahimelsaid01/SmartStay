using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class PropertyImageConfiguration
        : IEntityTypeConfiguration<PropertyImage>
    {
        public void Configure(
            EntityTypeBuilder<PropertyImage> builder)
        {
            builder.ToTable("PropertyImages");

            builder.HasKey(image =>
                image.Id);

            builder.Property(image =>
                    image.Url)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(image =>
                    image.PublicId)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(image =>
                    image.Format)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false);

            builder.Property(image =>
                    image.IsCover)
                .IsRequired();

            builder.Property(image =>
                    image.DisplayOrder)
                .IsRequired();

            builder.Property(image =>
                    image.CreatedAt)
                .IsRequired();

            builder.HasOne(image =>
                    image.Property)
                .WithMany(property =>
                    property.Images)
                .HasForeignKey(image =>
                    image.PropertyId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasIndex(image =>
                image.PropertyId);

            builder.HasIndex(image =>
                new
                {
                    image.PropertyId,
                    image.DisplayOrder
                });

            builder.HasIndex(image =>
                new
                {
                    image.PropertyId,
                    image.IsCover
                });
        }
    }
}