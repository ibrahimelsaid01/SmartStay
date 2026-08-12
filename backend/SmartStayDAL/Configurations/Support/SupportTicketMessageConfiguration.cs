using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class SupportTicketMessageConfiguration
        : IEntityTypeConfiguration<SupportTicketMessage>
    {
        public void Configure(
            EntityTypeBuilder<SupportTicketMessage> builder)
        {
            builder.ToTable(
                "SupportTicketMessages");

            builder.HasKey(
                message =>
                    message.Id);

            builder.Property(
                    message =>
                        message.Message)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(
                    message =>
                        message.IsAdminMessage)
                .IsRequired();

            builder.Property(
                    message =>
                        message.CreatedAt)
                .IsRequired();

            builder.HasOne(
                    message =>
                        message.SenderUser)
                .WithMany()
                .HasForeignKey(
                    message =>
                        message.SenderUserId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(
                    message =>
                        message.SupportTicketId);

            builder.HasIndex(
                    message =>
                        message.SenderUserId);

            builder.HasIndex(
                    message =>
                        message.CreatedAt);

            builder.ToTable(
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTicketMessages_Message_NotEmpty",
                        "LEN(LTRIM(RTRIM([Message]))) > 0");
                });
        }
    }
}