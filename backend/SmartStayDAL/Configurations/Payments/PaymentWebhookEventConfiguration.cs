using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public sealed class PaymentWebhookEventConfiguration
        : IEntityTypeConfiguration<PaymentWebhookEvent>
    {
        public void Configure(
            EntityTypeBuilder<PaymentWebhookEvent> builder)
        {
            builder.ToTable(
                "PaymentWebhookEvents",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_PaymentWebhookEvents_Provider_NotEmpty",
                        "LEN(LTRIM(RTRIM([Provider]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_PaymentWebhookEvents_ProviderEventId_NotEmpty",
                        "LEN(LTRIM(RTRIM([ProviderEventId]))) > 0");

                    tableBuilder.HasCheckConstraint(
                        "CK_PaymentWebhookEvents_EventType_NotEmpty",
                        "LEN(LTRIM(RTRIM([EventType]))) > 0");
                });

            builder.HasKey(webhookEvent =>
                webhookEvent.Id);

            builder.Property(webhookEvent =>
                    webhookEvent.Provider)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(webhookEvent =>
                    webhookEvent.ProviderEventId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(webhookEvent =>
                    webhookEvent.EventType)
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(webhookEvent =>
                    webhookEvent.ReceivedAt)
                .IsRequired();

            builder.Property(webhookEvent =>
                    webhookEvent.ProcessedAt)
                .IsRequired(false);

            /*
             * The same Stripe Event must never be processed
             * more than once.
             */
            builder.HasIndex(webhookEvent =>
                    new
                    {
                        webhookEvent.Provider,
                        webhookEvent.ProviderEventId
                    })
                .IsUnique()
                .HasDatabaseName(
                    "UX_PaymentWebhookEvents_Provider_EventId");

            builder.HasIndex(webhookEvent =>
                    webhookEvent.ReceivedAt)
                .HasDatabaseName(
                    "IX_PaymentWebhookEvents_ReceivedAt");
        }
    }
}