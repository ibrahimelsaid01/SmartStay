using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public class RefreshTokenConfiguration
        : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(
            EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(token => token.Id);

            builder.Property(token => token.TokenHash)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(token => token.ExpiresAt)
                .IsRequired();

            builder.Property(token => token.CreatedAt)
                .IsRequired();

            builder.Property(token => token.CreatedByIp)
                .HasMaxLength(45);

            builder.Property(token => token.RevokedByIp)
                .HasMaxLength(45);

            builder.Property(token => token.RevocationReason)
                .HasMaxLength(250);

            builder.HasIndex(token => token.TokenHash)
                .IsUnique();

            builder.HasIndex(token => token.UserId);

            builder.HasIndex(token => token.ExpiresAt);

            builder.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(token => token.ReplacedByToken)
                .WithOne()
                .HasForeignKey<RefreshToken>(
                    token => token.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}