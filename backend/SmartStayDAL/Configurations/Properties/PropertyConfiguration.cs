using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class PropertyConfiguration
        : IEntityTypeConfiguration<Property>
    {
        public void Configure(
            EntityTypeBuilder<Property> builder)
        {
            builder.ToTable("Properties");

            builder.HasKey(property =>
                property.Id);

            /*
             * Basic information.
             */

            builder.Property(property =>
                    property.Title)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(property =>
                    property.Description)
                .IsRequired()
                .HasMaxLength(3000);

            builder.Property(property =>
                    property.PropertyType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(property =>
                    property.SpaceType)
                .IsRequired()
                .HasConversion<int>();

            /*
             * Capacity.
             *
             * These values are nullable while the
             * property is an incomplete Draft.
             */

            builder.Property(property =>
                    property.MaxGuests)
                .IsRequired(false);

            builder.Property(property =>
                    property.Bedrooms)
                .IsRequired(false);

            builder.Property(property =>
                    property.Beds)
                .IsRequired(false);

            builder.Property(property =>
                    property.Bathrooms)
                .HasPrecision(3, 1)
                .IsRequired(false);

            /*
             * Pricing.
             */

            builder.Property(property =>
                    property.PricePerNight)
                .HasPrecision(18, 2)
                .IsRequired(false);

            builder.Property(property =>
                    property.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .IsUnicode(false);

            /*
             * Location.
             */

            builder.Property(property =>
                    property.Country)
                .HasMaxLength(100);

            builder.Property(property =>
                    property.City)
                .HasMaxLength(100);

            builder.Property(property =>
                    property.StreetAddress)
                .HasMaxLength(250);

            builder.Property(property =>
                    property.BuildingNumber)
                .HasMaxLength(30);

            builder.Property(property =>
                    property.Floor)
                .HasMaxLength(30);

            builder.Property(property =>
                    property.ApartmentNumber)
                .HasMaxLength(30);

            builder.Property(property =>
                    property.PostalCode)
                .HasMaxLength(20);

            builder.Property(property =>
                    property.Latitude)
                .HasPrecision(9, 6)
                .IsRequired(false);

            builder.Property(property =>
                    property.Longitude)
                .HasPrecision(9, 6)
                .IsRequired(false);

            /*
             * Policies and house rules.
             */
            builder.Property(property =>
                property.CancellationPolicy)
                .HasConversion<int>()
                .HasDefaultValue(
                    CancellationPolicyType.Moderate)
                .IsRequired(false);

            builder.Property(property =>
                    property.AdditionalHouseRules)
                .HasMaxLength(1000);

            /*
             * Property status and review data.
             */

            builder.Property(property =>
          property.Status)
      .IsRequired()
      .HasConversion<int>();

            builder.Property(property =>
                    property.RejectionReason)
                .HasMaxLength(500);

            builder.Property(property =>
                    property.CreatedAt)
                .IsRequired();

            /*
             * Relationships.
             */

            builder.HasOne(property =>
                    property.HostProfile)
                .WithMany(hostProfile =>
                    hostProfile.Properties)
                .HasForeignKey(property =>
                    property.HostProfileId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            /*
             * Indexes.
             */

            builder.HasIndex(property =>
                property.HostProfileId);

            builder.HasIndex(property =>
                property.Status);

            builder.HasIndex(property =>
                new
                {
                    property.HostProfileId,
                    property.Status
                });

            builder.HasIndex(property =>
                new
                {
                    property.Status,
                    property.Country,
                    property.City
                });
        }
    }
}