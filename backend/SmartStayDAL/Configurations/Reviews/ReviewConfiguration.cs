using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class ReviewConfiguration
        : IEntityTypeConfiguration<Review>
    {
        public void Configure(
            EntityTypeBuilder<Review> builder)
        {
            builder.ToTable(
                "Reviews",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_Reviews_Rating_Valid",
                        "[Rating] BETWEEN 1 AND 5");

                    /*
                     * ReviewStatus:
                     *
                     * 1 = Pending
                     * 2 = Posted
                     * 3 = Rejected
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Reviews_Status_Valid",
                        "[Status] IN (1, 2, 3)");

                    /*
                     * At least one comment must be provided.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Reviews_Comment_Required",
                        "LEN(LTRIM(RTRIM(" +
                        "ISNULL([PositiveComment], '')))) > 0 " +
                        "OR " +
                        "LEN(LTRIM(RTRIM(" +
                        "ISNULL([NegativeComment], '')))) > 0");

                    /*
                     * Pending reviews must not contain
                     * moderation result information.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Reviews_Pending_State_Valid",
                        "[Status] <> 1 OR (" +
                        "[ModeratedByUserId] IS NULL " +
                        "AND [ModeratedAt] IS NULL " +
                        "AND [PublishedAt] IS NULL " +
                        "AND [RejectedAt] IS NULL " +
                        "AND [RejectionReason] IS NULL)");

                    /*
                     * Posted reviews require complete
                     * approval information.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Reviews_Posted_State_Valid",
                        "[Status] <> 2 OR (" +
                        "[ModeratedByUserId] IS NOT NULL " +
                        "AND [ModeratedAt] IS NOT NULL " +
                        "AND [PublishedAt] IS NOT NULL " +
                        "AND [RejectedAt] IS NULL " +
                        "AND [RejectionReason] IS NULL)");

                    /*
                     * Rejected reviews require a reason.
                     */
                    tableBuilder.HasCheckConstraint(
                        "CK_Reviews_Rejected_State_Valid",
                        "[Status] <> 3 OR (" +
                        "[ModeratedByUserId] IS NOT NULL " +
                        "AND [ModeratedAt] IS NOT NULL " +
                        "AND [RejectedAt] IS NOT NULL " +
                        "AND [PublishedAt] IS NULL " +
                        "AND LEN(LTRIM(RTRIM(" +
                        "ISNULL([RejectionReason], '')))) > 0)");
                });

            builder.HasKey(review =>
                review.Id);

            builder.Property(review =>
                    review.Rating)
                .IsRequired();

            builder.Property(review =>
                    review.PositiveComment)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.Property(review =>
                    review.NegativeComment)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.Property(review =>
                    review.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(review =>
                    review.RejectionReason)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(review =>
                    review.CreatedAt)
                .IsRequired();

            builder.Property(review =>
                    review.UpdatedAt)
                .IsRequired(false);

            builder.Property(review =>
                    review.ModeratedAt)
                .IsRequired(false);

            builder.Property(review =>
                    review.PublishedAt)
                .IsRequired(false);

            builder.Property(review =>
                    review.RejectedAt)
                .IsRequired(false);

            /*
             * One booking can have only one review.
             */
            builder.HasOne(review =>
                    review.Booking)
                .WithOne()
                .HasForeignKey<Review>(review =>
                    review.BookingId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(review =>
                    review.Property)
                .WithMany()
                .HasForeignKey(review =>
                    review.PropertyId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(review =>
                    review.User)
                .WithMany()
                .HasForeignKey(review =>
                    review.UserId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasOne(review =>
                    review.ModeratedByUser)
                .WithMany()
                .HasForeignKey(review =>
                    review.ModeratedByUserId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(review =>
                    review.BookingId)
                .IsUnique()
                .HasDatabaseName(
                    "IX_Reviews_BookingId_Unique");

            /*
             * Used by public property review queries.
             */
            builder.HasIndex(review =>
                    new
                    {
                        review.PropertyId,
                        review.Status,
                        review.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_Reviews_Property_Status_CreatedAt");

            /*
             * Used by My Reviews.
             */
            builder.HasIndex(review =>
                    new
                    {
                        review.UserId,
                        review.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_Reviews_User_CreatedAt");

            /*
             * Used by admin moderation queries.
             */
            builder.HasIndex(review =>
                    new
                    {
                        review.Status,
                        review.CreatedAt
                    })
                .HasDatabaseName(
                    "IX_Reviews_Status_CreatedAt");

            builder.HasIndex(review =>
                    review.ModeratedByUserId)
                .HasDatabaseName(
                    "IX_Reviews_ModeratedByUserId");
        }
    }
}