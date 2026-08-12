using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class AmenityConfiguration
        : IEntityTypeConfiguration<Amenity>
    {
        public void Configure(
            EntityTypeBuilder<Amenity> builder)
        {
            builder.ToTable("Amenities");

            builder.HasKey(amenity =>
                amenity.Id);

            builder.Property(amenity =>
                    amenity.Code)
                .IsRequired()
                .HasMaxLength(60)
                .IsUnicode(false);

            builder.Property(amenity =>
                    amenity.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(amenity =>
                    amenity.Category)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(amenity =>
                    amenity.IconKey)
                .IsRequired()
                .HasMaxLength(60)
                .IsUnicode(false);

            builder.Property(amenity =>
                    amenity.DisplayOrder)
                .IsRequired();

            builder.Property(amenity =>
                    amenity.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(amenity =>
                    amenity.Code)
                .IsUnique();

            builder.HasIndex(amenity =>
                new
                {
                    amenity.IsActive,
                    amenity.Category,
                    amenity.DisplayOrder
                });
            builder.HasData(
                AmenitySeedData.All);
        }
    }
}