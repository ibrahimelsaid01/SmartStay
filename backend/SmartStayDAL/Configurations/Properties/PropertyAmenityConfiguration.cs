using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class PropertyAmenityConfiguration
        : IEntityTypeConfiguration<PropertyAmenity>
    {
        public void Configure(
            EntityTypeBuilder<PropertyAmenity> builder)
        {
            builder.ToTable("PropertyAmenities");

            builder.HasKey(propertyAmenity =>
                new
                {
                    propertyAmenity.PropertyId,
                    propertyAmenity.AmenityId
                });

            builder.Property(propertyAmenity =>
                    propertyAmenity.CreatedAt)
                .IsRequired();

            builder.HasOne(propertyAmenity =>
                    propertyAmenity.Property)
                .WithMany(property =>
                    property.PropertyAmenities)
                .HasForeignKey(propertyAmenity =>
                    propertyAmenity.PropertyId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasOne(propertyAmenity =>
                    propertyAmenity.Amenity)
                .WithMany(amenity =>
                    amenity.PropertyAmenities)
                .HasForeignKey(propertyAmenity =>
                    propertyAmenity.AmenityId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(propertyAmenity =>
                propertyAmenity.AmenityId);
        }
    }
}