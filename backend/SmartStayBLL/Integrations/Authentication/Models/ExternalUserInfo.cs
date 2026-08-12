namespace SmartStayBLL
{
    public sealed class ExternalUserInfo
    {
        public string Provider { get; set; } = string.Empty;

        public string ProviderKey { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool EmailVerified { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? ProfileImageUrl { get; set; }
    }
}