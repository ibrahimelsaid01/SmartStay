using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class ReviewReplyConfiguration
        : IEntityTypeConfiguration<ReviewReply>
    {
        public void Configure(
            EntityTypeBuilder<ReviewReply> builder)
        {
            builder.ToTable("ReviewReplies");

            builder.HasKey(reply =>
                reply.Id);

            builder.Property(reply =>
                    reply.Content)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(reply =>
                    reply.CreatedAt)
                .IsRequired();

            builder.Property(reply =>
                    reply.UpdatedAt)
                .IsRequired(false);

            /*
             * One review can have only one host reply.
             *
             * If a review is deleted, its reply is
             * deleted automatically.
             */
            builder.HasOne(reply =>
                    reply.Review)
                .WithOne(review =>
                    review.Reply)
                .HasForeignKey<ReviewReply>(reply =>
                    reply.ReviewId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasOne(reply =>
                    reply.HostProfile)
                .WithMany()
                .HasForeignKey(reply =>
                    reply.HostProfileId)
                .OnDelete(
                    DeleteBehavior.Restrict);

            builder.HasIndex(reply =>
                    reply.ReviewId)
                .IsUnique()
                .HasDatabaseName(
                    "IX_ReviewReplies_ReviewId_Unique");

            builder.HasIndex(reply =>
                    reply.HostProfileId)
                .HasDatabaseName(
                    "IX_ReviewReplies_HostProfileId");

            builder.HasIndex(reply =>
                    reply.CreatedAt)
                .HasDatabaseName(
                    "IX_ReviewReplies_CreatedAt");
        }
    }
}