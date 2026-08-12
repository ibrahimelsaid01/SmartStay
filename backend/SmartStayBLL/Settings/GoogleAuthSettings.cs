namespace SmartStayBLL
{
    public sealed class GoogleAuthSettings
    {
        public const string SectionName =
            "Authentication:Google";

        public string ClientId { get; set; } = string.Empty;
    }
}