namespace SmartStayBLL
{
    public interface IBookingLifecycleService
    {
        /*
         * Called internally after the payment provider
         * confirms that payment succeeded.
         */
        Task<BookingConfirmationResponse>
            ConfirmAfterSuccessfulPaymentAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default);

        /*
         * Processes all time-based lifecycle transitions:
         *
         * Pending   -> Expired
         * Confirmed -> Completed
         */
        Task<BookingLifecycleProcessResponse>
            ProcessLifecycleAsync(
                CancellationToken cancellationToken = default);
    }
}