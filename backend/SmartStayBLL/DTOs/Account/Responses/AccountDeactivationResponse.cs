namespace SmartStayBLL
{
    public sealed class AccountDeactivationResponse
    {
        public bool IsDeactivated { get; set; }

        public DateTimeOffset DeactivatedAt { get; set; }

        public int UnpublishedPropertiesCount { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}