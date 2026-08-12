namespace SmartStayBLL
{
    public static class NotificationDeduplicationKeys
    {
        public static string ReviewApproved(
            Guid reviewId)
        {
            return
                $"review-approved:{reviewId:N}";
        }

        public static string ReviewRejected(
            Guid reviewId)
        {
            return
                $"review-rejected:{reviewId:N}";
        }

        public static string ReviewReplyReceived(
            Guid reviewId)
        {
            return
                $"review-reply-received:{reviewId:N}";
        }

        public static string BookingPendingPayment(
            Guid bookingId)
        {
            return
                $"booking-pending-payment:{bookingId:N}";
        }

        public static string BookingConfirmed(
            Guid bookingId)
        {
            return
                $"booking-confirmed:{bookingId:N}";
        }

        public static string BookingCancelled(
            Guid bookingId)
        {
            return
                $"booking-cancelled:{bookingId:N}";
        }

        public static string BookingCompleted(
            Guid bookingId)
        {
            return
                $"booking-completed:{bookingId:N}";
        }

        public static string BookingExpired(
            Guid bookingId)
        {
            return
                $"booking-expired:{bookingId:N}";
        }

        public static string NewBookingReceived(
            Guid bookingId)
        {
            return
                $"new-booking-received:{bookingId:N}";
        }

        public static string PaymentSucceeded(
            Guid paymentId)
        {
            return
                $"payment-succeeded:{paymentId:N}";
        }

        public static string PaymentFailed(
            Guid paymentId)
        {
            return
                $"payment-failed:{paymentId:N}";
        }

        public static string PaymentCancelled(
            Guid paymentId)
        {
            return
                $"payment-cancelled:{paymentId:N}";
        }

        public static string PaymentPartiallyRefunded(
            Guid paymentId)
        {
            return
                $"payment-partially-refunded:{paymentId:N}";
        }

        public static string PaymentRefunded(
            Guid paymentId)
        {
            return
                $"payment-refunded:{paymentId:N}";
        }

        public static string HostApplicationApproved(
            Guid hostProfileId,
            DateTimeOffset reviewedAt)
        {
            return
                $"host-application-approved:" +
                $"{hostProfileId:N}:" +
                $"{reviewedAt.UtcDateTime.Ticks}";
        }

        public static string HostApplicationRejected(
            Guid hostProfileId,
            DateTimeOffset reviewedAt)
        {
            return
                $"host-application-rejected:" +
                $"{hostProfileId:N}:" +
                $"{reviewedAt.UtcDateTime.Ticks}";
        }

        public static string PropertyPublished(
            Guid propertyId,
            DateTimeOffset reviewedAt)
        {
            return
                $"property-published:" +
                $"{propertyId:N}:" +
                $"{reviewedAt.UtcDateTime.Ticks}";
        }

        public static string PropertyRejected(
            Guid propertyId,
            DateTimeOffset reviewedAt)
        {
            return
                $"property-rejected:" +
                $"{propertyId:N}:" +
                $"{reviewedAt.UtcDateTime.Ticks}";
        }
    }
}