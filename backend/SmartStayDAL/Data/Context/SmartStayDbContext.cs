using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SmartStayDAL
{
    public class SmartStayDbContext
        : IdentityDbContext<
            ApplicationUser,
            IdentityRole<Guid>,
            Guid>
    {
        public SmartStayDbContext(
            DbContextOptions<SmartStayDbContext> options)
            : base(options)
        {
        }

        public DbSet<OtpCode> OtpCodes =>
            Set<OtpCode>();

        public DbSet<RefreshToken> RefreshTokens =>
            Set<RefreshToken>();

        public DbSet<HostProfile> HostProfiles =>
            Set<HostProfile>();

        public DbSet<HostIdentityDocument>
            HostIdentityDocuments =>
                Set<HostIdentityDocument>();

        public DbSet<Property> Properties =>
            Set<Property>();

        public DbSet<PropertyImage> PropertyImages =>
            Set<PropertyImage>();

        public DbSet<Amenity> Amenities =>
            Set<Amenity>();

        public DbSet<PropertyAmenity> PropertyAmenities =>
            Set<PropertyAmenity>();

        public DbSet<PropertyVerificationDocument>
            PropertyVerificationDocuments =>
                Set<PropertyVerificationDocument>();

        public DbSet<PropertyVerificationDocumentPage>
            PropertyVerificationDocumentPages =>
                Set<PropertyVerificationDocumentPage>();

        public DbSet<Booking> Bookings =>
            Set<Booking>();

        public DbSet<BookingPayment> BookingPayments =>
            Set<BookingPayment>();

        public DbSet<BookingPayout> BookingPayouts =>
            Set<BookingPayout>();

        public DbSet<BookingPaymentRefund>
            BookingPaymentRefunds =>
                Set<BookingPaymentRefund>();

        public DbSet<PaymentWebhookEvent>
            PaymentWebhookEvents =>
                Set<PaymentWebhookEvent>();

        public DbSet<WishList> WishLists =>
            Set<WishList>();

        public DbSet<WishListItem> WishListItems =>
            Set<WishListItem>();

        public DbSet<Review> Reviews =>
            Set<Review>();

        public DbSet<ReviewReply> ReviewReplies =>
            Set<ReviewReply>();

        public DbSet<ReviewHelpfulVote>
            ReviewHelpfulVotes =>
                Set<ReviewHelpfulVote>();

        public DbSet<Notification> Notifications =>
            Set<Notification>();

        public DbSet<SupportTicket> SupportTickets { get; set; }

        public DbSet<SupportTicketMessage> SupportTicketMessages { get; set; }

        public DbSet<SupportTicketAttachment> SupportTicketAttachments { get; set; }

        public DbSet<UserBookingRestriction> UserBookingRestrictions =>
            Set<UserBookingRestriction>();

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(
                typeof(SmartStayDbContext).Assembly);
        }
    }
}