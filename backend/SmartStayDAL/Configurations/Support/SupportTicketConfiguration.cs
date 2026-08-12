using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class SupportTicketConfiguration
        : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(
            EntityTypeBuilder<SupportTicket> builder)
        {
            builder.ToTable(
                "SupportTickets");

            builder.HasKey(
                ticket =>
                    ticket.Id);

            builder.Property(
                    ticket =>
                        ticket.Subject)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(
                    ticket =>
                        ticket.Description)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(
                    ticket =>
                        ticket.Category)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    ticket =>
                        ticket.Urgency)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    ticket =>
                        ticket.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    ticket =>
                        ticket.DecisionStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    ticket =>
                        ticket.DecisionAction)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    ticket =>
                        ticket.DecisionNote)
                .HasMaxLength(1000);

            builder.Property(
                    ticket =>
                        ticket.ResolutionNote)
                .HasMaxLength(1000);

            builder.Property(
                    ticket =>
                        ticket.CreatedAt)
                .IsRequired();

            builder.Property(
                    ticket =>
                        ticket.UpdatedAt)
                .IsRequired();

            builder.HasOne(
                    ticket =>
                        ticket.CreatedByUser)
                .WithMany()
                .HasForeignKey(
                    ticket =>
                        ticket.CreatedByUserId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(
                    ticket =>
                        ticket.ResolvedByAdmin)
                .WithMany()
                .HasForeignKey(
                    ticket =>
                        ticket.ResolvedByAdminId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(
                    ticket =>
                        ticket.DecidedByAdmin)
                .WithMany()
                .HasForeignKey(
                    ticket =>
                        ticket.DecidedByAdminId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(
                    ticket =>
                        ticket.Booking)
                .WithMany()
                .HasForeignKey(
                    ticket =>
                        ticket.BookingId)
                .OnDelete(
                    DeleteBehavior.SetNull);

            builder.HasOne(
                    ticket =>
                        ticket.Property)
                .WithMany()
                .HasForeignKey(
                    ticket =>
                        ticket.PropertyId)
                .OnDelete(
                    DeleteBehavior.SetNull);

            builder.HasMany(
                    ticket =>
                        ticket.Messages)
                .WithOne(
                    message =>
                        message.SupportTicket)
                .HasForeignKey(
                    message =>
                        message.SupportTicketId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasMany(
                    ticket =>
                        ticket.Attachments)
                .WithOne(
                    attachment =>
                        attachment.SupportTicket)
                .HasForeignKey(
                    attachment =>
                        attachment.SupportTicketId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasIndex(
                    ticket =>
                        ticket.CreatedByUserId);

            builder.HasIndex(
                    ticket =>
                        ticket.BookingId);

            builder.HasIndex(
                    ticket =>
                        ticket.PropertyId);

            builder.HasIndex(
                    ticket =>
                        ticket.DecidedByAdminId);

            builder.HasIndex(
                    ticket =>
                        new
                        {
                            ticket.Status,
                            ticket.CreatedAt
                        });

            builder.HasIndex(
                    ticket =>
                        new
                        {
                            ticket.Category,
                            ticket.Urgency
                        });

            builder.HasIndex(
                    ticket =>
                        new
                        {
                            ticket.DecisionStatus,
                            ticket.DecisionAction
                        });

            builder.ToTable(
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_Subject_NotEmpty",
                        "LEN(LTRIM(RTRIM([Subject]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_Description_NotEmpty",
                        "LEN(LTRIM(RTRIM([Description]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_Category_Valid",
                        "[Category] BETWEEN 1 AND 9");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_Urgency_Valid",
                        "[Urgency] BETWEEN 1 AND 4");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_Status_Valid",
                        "[Status] BETWEEN 1 AND 4");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_DecisionStatus_Valid",
                        "[DecisionStatus] BETWEEN 1 AND 4");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_DecisionAction_Valid",
                        "[DecisionAction] BETWEEN 1 AND 7");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_Resolved_State",
                        "([Status] IN (3, 4) AND [ResolvedAt] IS NOT NULL) OR ([Status] IN (1, 2) AND [ResolvedAt] IS NULL)");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_ResolvedBy_Requires_ResolvedAt",
                        "([ResolvedByAdminId] IS NULL) OR ([ResolvedAt] IS NOT NULL)");

                    tableBuilder.HasCheckConstraint(
                        "CK_SupportTickets_DecidedBy_Requires_DecidedAt",
                        "([DecidedByAdminId] IS NULL) OR ([DecidedAt] IS NOT NULL)");
                });
        }
    }
}