namespace SmartStayBLL
{
    public sealed class FacebookAuthSettings
    {
        public const string SectionName =
            "Authentication:Facebook";

        public string AppId { get; set; } = string.Empty;

        public string AppSecret { get; set; } = string.Empty;

        public string GraphApiVersion { get; set; } = "v25.0";
    }
}