using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class ApplicationUserConfiguration
        : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(
            EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(user => user.FirstName)
                .HasMaxLength(100);

            builder.Property(user => user.LastName)
                .HasMaxLength(100);

            builder.Property(user => user.ProfileImageUrl)
                .HasMaxLength(500);

            builder.Property(user =>
                    user.ProfileImagePublicId)
                .HasMaxLength(500);

            builder.Property(user => user.Gender)
                .IsRequired(false);

            builder.Property(user => user.Birthday)
                .HasColumnType("date")
                .IsRequired(false);

            builder.Property(user => user.Country)
                .HasMaxLength(100);

            builder.Property(user => user.Address)
                .HasMaxLength(300);

            builder.Property(user => user.ZipCode)
                .HasMaxLength(20);

            builder.Property(user => user.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(user =>
                    user.IsProfileCompleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(user => user.CreatedAt)
                .IsRequired();

            builder.Property(user => user.UpdatedAt)
                .IsRequired(false);

            builder.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(user =>
                    user.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(user => user.UserName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(user =>
                    user.NormalizedUserName)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(user =>
                    user.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique();
        }
    }
}