namespace SmartStayBLL
{
    public sealed class AuthenticatedUserResponse
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? ProfileImageUrl { get; set; }

        public bool IsProfileCompleted { get; set; }

        public IList<string> Roles { get; set; }
            = new List<string>();
    }
}