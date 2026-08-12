namespace SmartStayDAL
{
    public enum PaymentRefundStatus
    {
        /*
         * SmartStay created the refund operation locally,
         * but its final provider result is not known yet.
         */
        Pending = 1,

        /*
         * The refund requires an additional action before
         * Stripe can finish processing it.
         */
        RequiresAction = 2,

        /*
         * Stripe confirmed that the refund succeeded.
         */
        Succeeded = 3,

        /*
         * Stripe reported that the refund failed.
         */
        Failed = 4,

        /*
         * The refund was cancelled before completion.
         */
        Cancelled = 5
    }
}