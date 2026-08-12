using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public class OtpCodeConfiguration
        : IEntityTypeConfiguration<OtpCode>
    {
        public void Configure(
            EntityTypeBuilder<OtpCode> builder)
        {
            builder.HasKey(otp => otp.Id);

            builder.Property(otp => otp.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(otp => otp.CodeHash)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(otp => otp.Purpose)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(otp => otp.ExpiresAt)
                .IsRequired();

            builder.Property(otp => otp.CreatedAt)
                .IsRequired();

            builder.Property(otp => otp.FailedAttempts)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasIndex(otp => new
            {
                otp.NormalizedEmail,
                otp.Purpose,
                otp.CreatedAt
            });

            builder.HasIndex(otp => otp.ExpiresAt);

            builder.HasOne(otp => otp.User)
                .WithMany(user => user.OtpCodes)
                .HasForeignKey(otp => otp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}