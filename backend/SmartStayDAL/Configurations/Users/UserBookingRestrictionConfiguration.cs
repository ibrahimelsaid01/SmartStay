using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class UserBookingRestrictionConfiguration
        : IEntityTypeConfiguration<UserBookingRestriction>
    {
        public void Configure(
            EntityTypeBuilder<UserBookingRestriction> builder)
        {
            builder.ToTable(
                "UserBookingRestrictions",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_UserBookingRestrictions_Type_Valid",
                        "[Type] BETWEEN 1 AND 3");

                    tableBuilder.HasCheckConstraint(
                        "CK_UserBookingRestrictions_Status_Valid",
                        "[Status] BETWEEN 1 AND 3");

                    tableBuilder.HasCheckConstraint(
                        "CK_UserBookingRestrictions_Reason_NotEmpty",
                        "LEN(LTRIM(RTRIM([Reason]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_UserBookingRestrictions_CancellationCount_NonNegative",
                        "[CancellationCountSnapshot] >= 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_UserBookingRestrictions_TemporaryRestriction_Requires_Until",
                        "([Type] <> 2) OR ([RestrictedUntil] IS NOT NULL)");

                    tableBuilder.HasCheckConstraint(
                        "CK_UserBookingRestrictions_RestrictedUntil_After_From",
                        "([RestrictedUntil] IS NULL) OR ([RestrictedUntil] > [RestrictedFrom])");

                    tableBuilder.HasCheckConstraint(
                        "CK_UserBookingRestrictions_Removed_State",
                        "([Status] <> 3) OR ([RemovedAt] IS NOT NULL)");
                });

            builder.HasKey(
                restriction =>
                    restriction.Id);

            builder.Property(
                    restriction =>
                        restriction.Type)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    restriction =>
                        restriction.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    restriction =>
                        restriction.Reason)
                .HasMaxLength(
                    1000)
                .IsRequired();

            builder.Property(
                    restriction =>
                        restriction.CancellationCountSnapshot)
                .IsRequired();

            builder.Property(
                    restriction =>
                        restriction.RestrictedFrom)
                .IsRequired();

            builder.Property(
                    restriction =>
                        restriction.RestrictedUntil)
                .IsRequired(
                    false);

            builder.Property(
                    restriction =>
                        restriction.CreatedBySystem)
                .HasDefaultValue(
                    true)
                .IsRequired();

            builder.Property(
                    restriction =>
                        restriction.CreatedAt)
                .IsRequired();

            builder.Property(
                    restriction =>
                        restriction.UpdatedAt)
                .IsRequired(
                    false);

            builder.Property(
                    restriction =>
                        restriction.RemovedAt)
                .IsRequired(
                    false);

            builder.Property(
                    restriction =>
                        restriction.RemovalNote)
                .HasMaxLength(
                    1000)
                .IsRequired(
                    false);

            builder.HasOne(
                    restriction =>
                        restriction.User)
                .WithMany()
                .HasForeignKey(
                    restriction =>
                        restriction.UserId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(
                    restriction =>
                        restriction.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(
                    restriction =>
                        restriction.CreatedByAdminId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(
                    restriction =>
                        restriction.RemovedByAdmin)
                .WithMany()
                .HasForeignKey(
                    restriction =>
                        restriction.RemovedByAdminId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(
                    restriction =>
                        restriction.UserId)
                .HasDatabaseName(
                    "IX_UserBookingRestrictions_UserId");

            builder.HasIndex(
                    restriction =>
                        new
                        {
                            restriction.UserId,
                            restriction.Status,
                            restriction.Type,
                            restriction.RestrictedUntil
                        })
                .HasDatabaseName(
                    "IX_UserBookingRestrictions_User_Status_Type_Until");

            builder.HasIndex(
                    restriction =>
                        new
                        {
                            restriction.Status,
                            restriction.CreatedAt
                        })
                .HasDatabaseName(
                    "IX_UserBookingRestrictions_Status_CreatedAt");

            builder.HasIndex(
                    restriction =>
                        restriction.CreatedByAdminId)
                .HasDatabaseName(
                    "IX_UserBookingRestrictions_CreatedByAdminId");

            builder.HasIndex(
                    restriction =>
                        restriction.RemovedByAdminId)
                .HasDatabaseName(
                    "IX_UserBookingRestrictions_RemovedByAdminId");
        }
    }
}