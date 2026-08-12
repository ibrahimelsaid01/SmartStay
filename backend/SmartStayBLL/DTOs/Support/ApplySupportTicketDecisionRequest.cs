namespace SmartStayBLL
{
    public sealed class ApplySupportTicketDecisionRequest
    {
        public string DecisionStatus { get; set; } =
            "NoDecision";

        public string DecisionAction { get; set; } =
            "NoAction";

        public string? DecisionNote { get; set; }

        public string? AdminMessage { get; set; }

        public bool ResolveTicket { get; set; }
    }
}