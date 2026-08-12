using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class AdminActionLogConfiguration
        : IEntityTypeConfiguration<AdminActionLog>
    {
        public void Configure(
            EntityTypeBuilder<AdminActionLog> builder)
        {
            builder.ToTable(
                "AdminActionLogs",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_AdminActionLogs_ActionType_Valid",
                        "[ActionType] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 99)");

                    tableBuilder.HasCheckConstraint(
                        "CK_AdminActionLogs_TargetType_Valid",
                        "[TargetType] BETWEEN 1 AND 11");

                    tableBuilder.HasCheckConstraint(
                        "CK_AdminActionLogs_Summary_NotEmpty",
                        "LEN(LTRIM(RTRIM([Summary]))) > 0");
                });

            builder.HasKey(
                log =>
                    log.Id);

            builder.Property(
                    log =>
                        log.ActionType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    log =>
                        log.TargetType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    log =>
                        log.TargetReference)
                .HasMaxLength(
                    200)
                .IsRequired(
                    false);

            builder.Property(
                    log =>
                        log.Summary)
                .HasMaxLength(
                    500)
                .IsRequired();

            builder.Property(
                    log =>
                        log.Details)
                .HasMaxLength(
                    4000)
                .IsRequired(
                    false);

            builder.Property(
                    log =>
                        log.MetadataJson)
                .HasMaxLength(
                    8000)
                .IsRequired(
                    false);

            builder.Property(
                    log =>
                        log.IpAddress)
                .HasMaxLength(
                    64)
                .IsRequired(
                    false);

            builder.Property(
                    log =>
                        log.UserAgent)
                .HasMaxLength(
                    512)
                .IsRequired(
                    false);

            builder.Property(
                    log =>
                        log.CreatedAt)
                .IsRequired();

            builder.HasOne(
                    log =>
                        log.AdminUser)
                .WithMany()
                .HasForeignKey(
                    log =>
                        log.AdminUserId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(
                    log =>
                        log.AdminUserId)
                .HasDatabaseName(
                    "IX_AdminActionLogs_AdminUserId");

            builder.HasIndex(
                    log =>
                        new
                        {
                            log.TargetType,
                            log.TargetId,
                            log.CreatedAt
                        })
                .HasDatabaseName(
                    "IX_AdminActionLogs_Target_CreatedAt");

            builder.HasIndex(
                    log =>
                        new
                        {
                            log.ActionType,
                            log.CreatedAt
                        })
                .HasDatabaseName(
                    "IX_AdminActionLogs_ActionType_CreatedAt");

            builder.HasIndex(
                    log =>
                        log.CreatedAt)
                .HasDatabaseName(
                    "IX_AdminActionLogs_CreatedAt");
        }
    }
}