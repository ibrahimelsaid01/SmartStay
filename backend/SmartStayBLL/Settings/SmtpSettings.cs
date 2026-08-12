namespace SmartStayBLL
{
    public sealed class SmtpSettings
    {
        public const string SectionName = "SmtpSettings";

        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromName { get; set; } = "SmartStay";

        public bool EnableSsl { get; set; } = true;
    }
}