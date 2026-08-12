using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class HostIdentityDocumentConfiguration
        : IEntityTypeConfiguration<HostIdentityDocument>
    {
        public void Configure(
            EntityTypeBuilder<HostIdentityDocument> builder)
        {
            builder.HasKey(document =>
                document.Id);

            builder.Property(document =>
                    document.FrontPublicId)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(document =>
                    document.FrontFormat)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(document =>
                    document.BackPublicId)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(document =>
                    document.BackFormat)
                .IsRequired()
                .HasMaxLength(10);

            builder.HasIndex(document =>
                    document.HostProfileId)
                .IsUnique();

            builder.HasOne(document =>
                    document.HostProfile)
                .WithOne(hostProfile =>
                    hostProfile.IdentityDocument)
                .HasForeignKey<HostIdentityDocument>(
                    document =>
                        document.HostProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}