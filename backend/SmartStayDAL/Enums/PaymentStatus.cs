namespace SmartStayDAL
{
    public enum PaymentStatus
    {
        /*
         * The payment attempt was created and is still
         * waiting for a final provider result.
         */
        Pending = 1,

        /*
         * The payment provider confirmed that the full
         * booking amount was paid successfully.
         */
        Succeeded = 2,

        /*
         * The payment provider rejected or failed
         * the payment attempt.
         */
        Failed = 3,

        /*
         * The payment attempt was cancelled before
         * completing successfully.
         */
        Cancelled = 4,

        /*
         * A portion of the successful payment
         * was refunded.
         */
        PartiallyRefunded = 5,

        /*
         * The full payment amount was refunded.
         */
        Refunded = 6
    }
}