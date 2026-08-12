using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class BookingConfiguration
        : IEntityTypeConfiguration<Booking>
    {
        public void Configure(
            EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable(
                "Bookings",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_CheckOutDate_After_CheckInDate",
                        "[CheckOutDate] > [CheckInDate]");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_GuestsCount_Positive",
                        "[GuestsCount] > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_Nights_Positive",
                        "[Nights] > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_PricePerNight_NonNegative",
                        "[PricePerNight] >= 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_Subtotal_NonNegative",
                        "[Subtotal] >= 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_ServiceFee_NonNegative",
                        "[ServiceFee] >= 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_TotalAmount_NonNegative",
                        "[TotalAmount] >= 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_CancellationPolicySnapshot_Valid",
                        "[CancellationPolicySnapshot] IN (1, 2, 3)");

                    /*
                     * BookingStatus:
                     *
                     * 1 = Pending
                     * 2 = Confirmed
                     * 3 = Cancelled
                     * 4 = Completed
                     * 5 = Expired
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_Status_Valid",
                        "[Status] IN (1, 2, 3, 4, 5)");

                    /*
                     * Every Pending booking must have a
                     * payment expiration time.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_Pending_Requires_ExpiresAt",
                        "[Status] <> 1 OR [ExpiresAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_Confirmed_Requires_ConfirmedAt",
                        "[Status] <> 2 OR [ConfirmedAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_Cancelled_Requires_CancelledAt",
                        "[Status] <> 3 OR [CancelledAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_Completed_Requires_CompletedAt",
                        "[Status] <> 4 OR [CompletedAt] IS NOT NULL");

                    tableBuilder.HasCheckConstraint(
                        "CK_Bookings_Expired_Requires_ExpiredAt",
                        "[Status] <> 5 OR [ExpiredAt] IS NOT NULL");
                });

            builder.HasKey(booking =>
                booking.Id);

            builder.Property(booking =>
                    booking.CheckInDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(booking =>
                    booking.CheckOutDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(booking =>
                    booking.GuestsCount)
                .IsRequired();

            builder.Property(booking =>
                    booking.Nights)
                .IsRequired();

            builder.Property(booking =>
                    booking.PricePerNight)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(booking =>
                    booking.Subtotal)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(booking =>
                    booking.ServiceFee)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(booking =>
                    booking.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(booking =>
                    booking.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(booking =>
                    booking.CancellationPolicySnapshot)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(booking =>
                    booking.AcceptedBookingTerms)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(booking =>
                    booking.AcceptedCancellationPolicy)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(booking =>
                    booking.AcceptedPropertyRules)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(booking =>
                    booking.AcceptedComplaintPolicy)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(booking =>
                    booking.BookingTermsAcceptedAt)
                .IsRequired(false);

            builder.Property(booking =>
                    booking.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(booking =>
                    booking.CancellationReason)
                .HasMaxLength(500);

            /*
             * Database fallback.
             *
             * The application service will also set this
             * value explicitly in the next step.
             */
            builder.Property(booking =>
                    booking.ExpiresAt)
                .HasDefaultValueSql(
                    "DATEADD(MINUTE, 15, " +
                    "TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'))");

            builder.Property(booking =>
                    booking.CreatedAt)
                .IsRequired();

            builder.Property(booking =>
                    booking.UpdatedAt)
                .IsRequired(false);

            builder.Property(booking =>
                    booking.ConfirmedAt)
                .IsRequired(false);

            builder.Property(booking =>
                    booking.CancelledAt)
                .IsRequired(false);

            builder.Property(booking =>
                    booking.ExpiredAt)
                .IsRequired(false);

            builder.Property(booking =>
                    booking.CompletedAt)
                .IsRequired(false);

            builder.HasOne(booking =>
                    booking.Property)
                .WithMany()
                .HasForeignKey(booking =>
                    booking.PropertyId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(booking =>
                    booking.GuestUser)
                .WithMany()
                .HasForeignKey(booking =>
                    booking.GuestUserId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            /*
             * Used by availability conflict checks.
             */
            builder.HasIndex(booking =>
                    new
                    {
                        booking.PropertyId,
                        booking.Status,
                        booking.CheckInDate,
                        booking.CheckOutDate
                    })
                .HasDatabaseName(
                    "IX_Bookings_Property_Status_Dates");

            builder.HasIndex(booking =>
                    new
                    {
                        booking.GuestUserId,
                        booking.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_Bookings_Guest_CreatedAt");

            builder.HasIndex(booking =>
                    booking.Status)
                .HasDatabaseName(
                    "IX_Bookings_Status");

            /*
             * Used by the automatic pending-booking
             * expiration process.
             */
            builder.HasIndex(booking =>
                    new
                    {
                        booking.Status,
                        booking.ExpiresAt
                    })
                .HasDatabaseName(
                    "IX_Bookings_Status_ExpiresAt");

            /*
             * Used by the automatic completion process.
             */
            builder.HasIndex(booking =>
                    new
                    {
                        booking.Status,
                        booking.CheckOutDate
                    })
                .HasDatabaseName(
                    "IX_Bookings_Status_CheckOutDate");
        }
    }
}