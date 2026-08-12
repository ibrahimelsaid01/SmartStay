using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class BookingPayoutConfiguration
        : IEntityTypeConfiguration<BookingPayout>
    {
        public void Configure(
            EntityTypeBuilder<BookingPayout> builder)
        {
            builder.ToTable(
                "BookingPayouts",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayouts_Amount_Positive",
                        "[Amount] > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayouts_Currency_Length",
                        "LEN([Currency]) = 3");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayouts_Status_Valid",
                        "[Status] IN (1, 2, 3, 4, 5, 6)");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayouts_Held_Requires_HeldAt",
                        "[Status] <> 2 OR [HeldAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayouts_Paid_Requires_PaidAt",
                        "[Status] <> 4 OR [PaidAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayouts_Blocked_Requires_BlockedAt",
                        "[Status] <> 5 OR [BlockedAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayouts_Refunded_Requires_RefundedAt",
                        "[Status] <> 6 OR [RefundedAt] IS NOT NULL");
                });

            builder.HasKey(
                payout =>
                    payout.Id);

            builder.Property(
                    payout =>
                        payout.Amount)
                .HasPrecision(
                    18,
                    2)
                .IsRequired();

            builder.Property(
                    payout =>
                        payout.Currency)
                .HasMaxLength(
                    3)
                .IsUnicode(
                    false)
                .IsRequired();

            builder.Property(
                    payout =>
                        payout.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(
                    payout =>
                        payout.AvailableAt)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.HeldAt)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.HoldReason)
                .HasMaxLength(
                    1000)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.ReleasedAt)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.ReleaseNote)
                .HasMaxLength(
                    1000)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.PaidAt)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.BlockedAt)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.BlockReason)
                .HasMaxLength(
                    1000)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.RefundedAt)
                .IsRequired(
                    false);

            builder.Property(
                    payout =>
                        payout.CreatedAt)
                .IsRequired();

            builder.Property(
                    payout =>
                        payout.UpdatedAt)
                .IsRequired(
                    false);

            builder.HasOne(
                    payout =>
                        payout.Booking)
                .WithOne()
                .HasForeignKey<BookingPayout>(
                    payout =>
                        payout.BookingId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(
                    payout =>
                        payout.BookingPayment)
                .WithOne()
                .HasForeignKey<BookingPayout>(
                    payout =>
                        payout.BookingPaymentId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(
                    payout =>
                        payout.HostProfile)
                .WithMany()
                .HasForeignKey(
                    payout =>
                        payout.HostProfileId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            /*
             * One payout record per booking.
             */
            builder.HasIndex(
                    payout =>
                        payout.BookingId)
                .IsUnique()
                .HasDatabaseName(
                    "UX_BookingPayouts_BookingId");

            /*
             * One payout record per successful payment.
             */
            builder.HasIndex(
                    payout =>
                        payout.BookingPaymentId)
                .IsUnique()
                .HasDatabaseName(
                    "UX_BookingPayouts_BookingPaymentId");

            builder.HasIndex(
                    payout =>
                        payout.HostProfileId)
                .HasDatabaseName(
                    "IX_BookingPayouts_HostProfileId");

            builder.HasIndex(
                    payout =>
                        new
                        {
                            payout.Status,
                            payout.AvailableAt
                        })
                .HasDatabaseName(
                    "IX_BookingPayouts_Status_AvailableAt");

            builder.HasIndex(
                    payout =>
                        new
                        {
                            payout.HostProfileId,
                            payout.Status,
                            payout.CreatedAt
                        })
                .HasDatabaseName(
                    "IX_BookingPayouts_Host_Status_CreatedAt");
        }
    }
}