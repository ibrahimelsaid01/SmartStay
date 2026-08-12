namespace SmartStayBLL
{
    public sealed class CreateSupportTicketRefundRequest
    {
        public decimal? RefundAmount { get; set; }

        public string? RefundNote { get; set; }
    }
}