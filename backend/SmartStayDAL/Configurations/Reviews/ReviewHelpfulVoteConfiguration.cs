using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class ReviewHelpfulVoteConfiguration
        : IEntityTypeConfiguration<ReviewHelpfulVote>
    {
        public void Configure(
            EntityTypeBuilder<ReviewHelpfulVote> builder)
        {
            builder.ToTable("ReviewHelpfulVotes");

            /*
             * Prevents the same user from voting
             * more than once on the same review.
             */
            builder.HasKey(vote =>
                new
                {
                    vote.ReviewId,
                    vote.UserId
                });

            builder.Property(vote =>
                    vote.CreatedAt)
                .IsRequired();

            /*
             * Deleting a review deletes its votes.
             */
            builder.HasOne(vote =>
                    vote.Review)
                .WithMany(review =>
                    review.HelpfulVotes)
                .HasForeignKey(vote =>
                    vote.ReviewId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            /*
             * Deleting a user deletes only that user's
             * helpful votes.
             */
            builder.HasOne(vote =>
                    vote.User)
                .WithMany()
                .HasForeignKey(vote =>
                    vote.UserId)
                .OnDelete(
                    DeleteBehavior.Cascade);

            builder.HasIndex(vote =>
                    vote.UserId)
                .HasDatabaseName(
                    "IX_ReviewHelpfulVotes_UserId");

            builder.HasIndex(vote =>
                    vote.CreatedAt)
                .HasDatabaseName(
                    "IX_ReviewHelpfulVotes_CreatedAt");
        }
    }
}