namespace SmartStayDAL
{
    public enum NotificationType
    {
        BookingPendingPayment = 1,

        BookingConfirmed = 2,

        BookingCancelled = 3,

        BookingCompleted = 4,

        BookingExpired = 5,

        NewBookingReceived = 6,

        PaymentSucceeded = 7,

        PaymentFailed = 8,

        PaymentRefunded = 9,

        HostApplicationApproved = 10,

        HostApplicationRejected = 11,

        PropertyPublished = 12,

        PropertyRejected = 13,

        ReviewApproved = 14,

        ReviewRejected = 15,

        ReviewReplyReceived = 16
    }
}