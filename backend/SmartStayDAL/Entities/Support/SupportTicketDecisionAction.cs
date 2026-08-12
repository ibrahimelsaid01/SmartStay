namespace SmartStayDAL
{
    public enum SupportTicketDecisionAction
    {
        NoAction = 1,

        PartialRefundRecommended = 2,

        FullRefundRecommended = 3,

        HostWarningRecommended = 4,

        HidePropertyRecommended = 5,

        HoldPayoutRecommended = 6,

        ReleasePayoutRecommended = 7
    }
}