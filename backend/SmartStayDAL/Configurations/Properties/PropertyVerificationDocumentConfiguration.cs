using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class
        PropertyVerificationDocumentConfiguration
        : IEntityTypeConfiguration<
            PropertyVerificationDocument>
    {
        public void Configure(
            EntityTypeBuilder<
                PropertyVerificationDocument> builder)
        {
            builder.ToTable(
                "PropertyVerificationDocuments");

            builder.HasKey(document =>
                document.Id);

            builder.Property(document =>
                    document.DocumentType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(document =>
                    document.CreatedAt)
                .IsRequired();

            builder.HasOne(document =>
                    document.Property)
                .WithOne(property =>
                    property.VerificationDocument)
                .HasForeignKey<
                    PropertyVerificationDocument>(
                        document =>
                            document.PropertyId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasIndex(document =>
                    document.PropertyId)
                .IsUnique();
        }
    }
}