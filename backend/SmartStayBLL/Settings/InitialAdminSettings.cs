namespace SmartStayBLL
{
    public sealed class InitialAdminSettings
    {
        public const string SectionName = "InitialAdmin";

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }
}