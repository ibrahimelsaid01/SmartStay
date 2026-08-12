using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class HostProfileConfiguration
        : IEntityTypeConfiguration<HostProfile>
    {
        public void Configure(
            EntityTypeBuilder<HostProfile> builder)
        {
            builder.HasKey(hostProfile =>
                hostProfile.Id);

            builder.Property(hostProfile =>
                    hostProfile.DisplayName)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(hostProfile =>
                    hostProfile.Bio)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(hostProfile =>
                    hostProfile.Country)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(hostProfile =>
                    hostProfile.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(hostProfile =>
                    hostProfile.ProfileImageUrl)
                .HasMaxLength(2048);

            builder.Property(hostProfile =>
                    hostProfile.ProfileImagePublicId)
                .HasMaxLength(500);

            builder.Property(hostProfile =>
                    hostProfile.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(hostProfile =>
                    hostProfile.RejectionReason)
                .HasMaxLength(500);

            builder.HasIndex(hostProfile =>
                    hostProfile.UserId)
                .IsUnique();

            builder.HasOne(hostProfile =>
                    hostProfile.User)
                .WithOne(user =>
                    user.HostProfile)
                .HasForeignKey<HostProfile>(
                    hostProfile =>
                        hostProfile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}