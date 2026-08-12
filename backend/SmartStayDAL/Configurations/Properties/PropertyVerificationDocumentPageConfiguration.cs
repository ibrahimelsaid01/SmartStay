using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class
        PropertyVerificationDocumentPageConfiguration
        : IEntityTypeConfiguration<
            PropertyVerificationDocumentPage>
    {
        public void Configure(
            EntityTypeBuilder<
                PropertyVerificationDocumentPage> builder)
        {
            builder.ToTable(
                "PropertyVerificationDocumentPages");

            builder.HasKey(page =>
                page.Id);

            builder.Property(page =>
                    page.PublicId)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(page =>
                    page.Format)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false);

            builder.Property(page =>
                    page.PageNumber)
                .IsRequired();

            builder.Property(page =>
                    page.CreatedAt)
                .IsRequired();

            builder.HasOne(page =>
                    page.VerificationDocument)
                .WithMany(document =>
                    document.Pages)
                .HasForeignKey(page =>
                    page.VerificationDocumentId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasIndex(page =>
                page.VerificationDocumentId);

            builder.HasIndex(page =>
                new
                {
                    page.VerificationDocumentId,
                    page.PageNumber
                })
                .IsUnique();
        }
    }
}