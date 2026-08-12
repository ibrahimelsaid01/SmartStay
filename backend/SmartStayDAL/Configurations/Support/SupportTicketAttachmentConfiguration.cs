using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class SupportTicketAttachmentConfiguration
        : IEntityTypeConfiguration<SupportTicketAttachment>
    {
        public void Configure(
            EntityTypeBuilder<SupportTicketAttachment> builder)
        {
            builder.ToTable(
                "SupportTicketAttachments");

            builder.HasKey(
                attachment =>
                    attachment.Id);

            builder.Property(
                    attachment =>
                        attachment.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    attachment =>
                        attachment.Url)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(
                    attachment =>
                        attachment.PublicId)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(
                    attachment =>
                        attachment.FileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(
                    attachment =>
                        attachment.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(
                    attachment =>
                        attachment.FileSizeInBytes)
                .IsRequired();

            builder.Property(
                    attachment =>
                        attachment.CreatedAt)
                .IsRequired();

            builder.HasOne(
                    attachment =>
                        attachment.SupportTicket)
                .WithMany(
                    ticket =>
                        ticket.Attachments)
                .HasForeignKey(
                    attachment =>
                        attachment.SupportTicketId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasOne(
                    attachment =>
                        attachment.UploadedByUser)
                .WithMany()
                .HasForeignKey(
                    attachment =>
                        attachment.UploadedByUserId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(
                    attachment =>
                        attachment.SupportTicketId)
                .HasDatabaseName(
                    "IX_SupportTicketAttachments_TicketId");

            builder.HasIndex(
                    attachment =>
                        attachment.UploadedByUserId)
                .HasDatabaseName(
                    "IX_SupportTicketAttachments_UploadedByUserId");

            builder.HasIndex(
                    attachment =>
                        attachment.CreatedAt)
                .HasDatabaseName(
                    "IX_SupportTicketAttachments_CreatedAt");

            builder.ToTable(
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTicketAttachments_Type_Valid",
                        "[Type] BETWEEN 1 AND 5");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTicketAttachments_Url_NotEmpty",
                        "LEN(LTRIM(RTRIM([Url]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTicketAttachments_PublicId_NotEmpty",
                        "LEN(LTRIM(RTRIM([PublicId]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTicketAttachments_FileName_NotEmpty",
                        "LEN(LTRIM(RTRIM([FileName]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTicketAttachments_ContentType_NotEmpty",
                        "LEN(LTRIM(RTRIM([ContentType]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTicketAttachments_FileSize_Positive",
                        "[FileSizeInBytes] > 0");
                });
        }
    }
}