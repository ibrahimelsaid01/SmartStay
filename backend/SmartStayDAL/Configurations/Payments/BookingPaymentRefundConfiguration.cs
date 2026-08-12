using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class BookingPaymentRefundConfiguration
        : IEntityTypeConfiguration<BookingPaymentRefund>
    {
        public void Configure(
            EntityTypeBuilder<BookingPaymentRefund> builder)
        {
            builder.ToTable(
                "BookingPaymentRefunds",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_Amount_Positive",
                        "[Amount] > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_Currency_Length",
                        "LEN([Currency]) = 3");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_Provider_NotEmpty",
                        "LEN(LTRIM(RTRIM([Provider]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_IdempotencyKey_NotEmpty",
                        "LEN(LTRIM(RTRIM([IdempotencyKey]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_ProviderRefundId_NotEmpty",
                        "[ProviderRefundId] IS NULL " +
                        "OR LEN(LTRIM(RTRIM([ProviderRefundId]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_Status_Valid",
                        "[Status] IN (1, 2, 3, 4, 5)");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_Succeeded_Requires_SucceededAt",
                        "[Status] <> 3 " +
                        "OR [SucceededAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_Failed_Requires_FailedAt",
                        "[Status] <> 4 " +
                        "OR [FailedAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_Cancelled_Requires_CancelledAt",
                        "[Status] <> 5 " +
                        "OR [CancelledAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_SucceededAt_Status_Valid",
                        "[SucceededAt] IS NULL " +
                        "OR [Status] = 3");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_FailedAt_Status_Valid",
                        "[FailedAt] IS NULL " +
                        "OR [Status] = 4");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_CancelledAt_Status_Valid",
                        "[CancelledAt] IS NULL " +
                        "OR [Status] = 5");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_UpdatedAt_Valid",
                        "[UpdatedAt] IS NULL " +
                        "OR [UpdatedAt] >= [CreatedAt]");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_SucceededAt_Valid",
                        "[SucceededAt] IS NULL " +
                        "OR [SucceededAt] >= [CreatedAt]");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_FailedAt_Valid",
                        "[FailedAt] IS NULL " +
                        "OR [FailedAt] >= [CreatedAt]");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPaymentRefunds_CancelledAt_Valid",
                        "[CancelledAt] IS NULL " +
                        "OR [CancelledAt] >= [CreatedAt]");
                });

            builder.HasKey(refund =>
                refund.Id);

            builder.Property(refund =>
                    refund.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(refund =>
                    refund.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(refund =>
                    refund.Provider)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(refund =>
                    refund.IdempotencyKey)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(refund =>
                    refund.ProviderRefundId)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired(false);

            /*
             * No database default here.
             *
             * The entity itself initializes Status to Pending.
             * This avoids EF Core enum default-value warnings.
             */
            builder.Property(refund =>
                    refund.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(refund =>
                    refund.FailureReason)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(refund =>
                    refund.CreatedAt)
                .IsRequired();

            builder.Property(refund =>
                    refund.UpdatedAt)
                .IsRequired(false);

            builder.Property(refund =>
                    refund.SucceededAt)
                .IsRequired(false);

            builder.Property(refund =>
                    refund.FailedAt)
                .IsRequired(false);

            builder.Property(refund =>
                    refund.CancelledAt)
                .IsRequired(false);

            builder.HasOne(refund =>
                    refund.BookingPayment)
                .WithMany(payment =>
                    payment.Refunds)
                .HasForeignKey(refund =>
                    refund.BookingPaymentId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(refund =>
                    new
                    {
                        refund.BookingPaymentId,
                        refund.IdempotencyKey
                    })
                .IsUnique()
                .HasDatabaseName(
                    "UX_BookingPaymentRefunds_Payment_IdempotencyKey");

            builder.HasIndex(refund =>
                    new
                    {
                        refund.Provider,
                        refund.ProviderRefundId
                    })
                .IsUnique()
                .HasFilter(
                    "[ProviderRefundId] IS NOT NULL")
                .HasDatabaseName(
                    "UX_BookingPaymentRefunds_Provider_RefundId");

            builder.HasIndex(refund =>
                    new
                    {
                        refund.BookingPaymentId,
                        refund.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_BookingPaymentRefunds_Payment_CreatedAt");

            builder.HasIndex(refund =>
                    new
                    {
                        refund.Status,
                        refund.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_BookingPaymentRefunds_Status_CreatedAt");
        }
    }
}