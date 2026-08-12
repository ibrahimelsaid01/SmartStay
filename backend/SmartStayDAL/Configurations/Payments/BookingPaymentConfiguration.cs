using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class BookingPaymentConfiguration
        : IEntityTypeConfiguration<BookingPayment>
    {
        public void Configure(
            EntityTypeBuilder<BookingPayment> builder)
        {
            builder.ToTable(
                "BookingPayments",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_Amount_Positive",
                        "[Amount] > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_Currency_Length",
                        "LEN([Currency]) = 3");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_Provider_NotEmpty",
                        "LEN(LTRIM(RTRIM([Provider]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_IdempotencyKey_NotEmpty",
                        "LEN(LTRIM(RTRIM([IdempotencyKey]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_RefundedAmount_Valid",
                        "[RefundedAmount] >= 0 " +
                        "AND [RefundedAmount] <= [Amount]");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_Status_Valid",
                        "[Status] IN (1, 2, 3, 4, 5, 6)");

                    /*
                     * A payment whose current status is
                     * Succeeded must have a success timestamp.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_Succeeded_Requires_SucceededAt",
                        "[Status] <> 2 " +
                        "OR [SucceededAt] IS NOT NULL");

                    /*
                     * SucceededAt can remain populated after
                     * the payment becomes partially or fully
                     * refunded.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_SucceededAt_Status_Valid",
                        "[SucceededAt] IS NULL " +
                        "OR [Status] IN (2, 5, 6)");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_Failed_Requires_FailedAt",
                        "[Status] <> 3 " +
                        "OR [FailedAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_Cancelled_Requires_CancelledAt",
                        "[Status] <> 4 " +
                        "OR [CancelledAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_PartialRefund_Valid",
                        "[Status] <> 5 OR " +
                        "(" +
                        "[SucceededAt] IS NOT NULL " +
                        "AND [RefundedAt] IS NOT NULL " +
                        "AND [RefundedAmount] > 0 " +
                        "AND [RefundedAmount] < [Amount]" +
                        ")");

                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_FullRefund_Valid",
                        "[Status] <> 6 OR " +
                        "(" +
                        "[SucceededAt] IS NOT NULL " +
                        "AND [RefundedAt] IS NOT NULL " +
                        "AND [RefundedAmount] = [Amount]" +
                        ")");

                    /*
                     * Pending, Succeeded, Failed and Cancelled
                     * payments must not contain refund data.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_BookingPayments_NonRefund_Status_Valid",
                        "[Status] IN (5, 6) " +
                        "OR " +
                        "(" +
                        "[RefundedAmount] = 0 " +
                        "AND [RefundedAt] IS NULL" +
                        ")");
                });

            builder.HasKey(payment =>
                payment.Id);

            builder.Property(payment =>
                    payment.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(payment =>
                    payment.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(payment =>
                    payment.Provider)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(payment =>
                    payment.IdempotencyKey)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(payment =>
                    payment.ProviderPaymentId)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(payment =>
                    payment.ProviderReference)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(payment =>
        payment.Status)
    .HasConversion<int>()
    .IsRequired();

            builder.Property(payment =>
                    payment.RefundedAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(payment =>
                    payment.FailureCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(payment =>
                    payment.FailureMessage)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(payment =>
                    payment.CreatedAt)
                .IsRequired();

            builder.Property(payment =>
                    payment.UpdatedAt)
                .IsRequired(false);

            builder.Property(payment =>
                    payment.SucceededAt)
                .IsRequired(false);

            builder.Property(payment =>
                    payment.FailedAt)
                .IsRequired(false);

            builder.Property(payment =>
                    payment.CancelledAt)
                .IsRequired(false);

            builder.Property(payment =>
                    payment.RefundedAt)
                .IsRequired(false);

            builder.HasOne(payment =>
                    payment.Booking)
                .WithMany()
                .HasForeignKey(payment =>
                    payment.BookingId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            /*
             * The same request can be retried safely for the
             * same booking without creating a duplicate.
             */
            builder.HasIndex(payment =>
                    new
                    {
                        payment.BookingId,
                        payment.IdempotencyKey
                    })
                .IsUnique()
                .HasDatabaseName(
                    "UX_BookingPayments_Booking_IdempotencyKey");

            /*
             * Only one active Pending payment attempt is
             * allowed for each booking.
             *
             * An explicit model index name is required because
             * another index below also uses BookingId.
             */
            builder.HasIndex(
                    payment =>
                        payment.BookingId,
                    "UX_BookingPayments_Booking_Pending")
                .IsUnique()
                .HasFilter(
                    "[Status] = 1")
                .HasDatabaseName(
                    "UX_BookingPayments_Booking_Pending");

            /*
             * Only one financially successful payment is
             * allowed for each booking.
             *
             * SucceededAt remains populated when the payment
             * becomes PartiallyRefunded or Refunded.
             *
             * An explicit model index name distinguishes this
             * index from the Pending index above.
             */
            builder.HasIndex(
                    payment =>
                        payment.BookingId,
                    "UX_BookingPayments_Booking_Succeeded")
                .IsUnique()
                .HasFilter(
                    "[SucceededAt] IS NOT NULL")
                .HasDatabaseName(
                    "UX_BookingPayments_Booking_Succeeded");

            /*
             * A payment identifier returned by one provider
             * must never be stored more than once.
             *
             * Multiple null values are permitted before the
             * provider creates the external payment.
             */
            builder.HasIndex(payment =>
                    new
                    {
                        payment.Provider,
                        payment.ProviderPaymentId
                    })
                .IsUnique()
                .HasFilter(
                    "[ProviderPaymentId] IS NOT NULL")
                .HasDatabaseName(
                    "UX_BookingPayments_Provider_PaymentId");

            /*
             * Supports listing all payment attempts for a
             * booking in chronological order.
             */
            builder.HasIndex(payment =>
                    new
                    {
                        payment.BookingId,
                        payment.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_BookingPayments_Booking_CreatedAt");

            /*
             * Supports payment monitoring and reconciliation
             * queries based on status and creation time.
             */
            builder.HasIndex(payment =>
                    new
                    {
                        payment.Status,
                        payment.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_BookingPayments_Status_CreatedAt");
        }
    }
}