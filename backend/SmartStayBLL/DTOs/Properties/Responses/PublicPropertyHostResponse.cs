namespace SmartStayBLL
{
    public sealed class PublicPropertyHostResponse
    {
        public Guid UserId { get; set; }

        public string FirstName { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;

        public string DisplayName { get; set; } =
            string.Empty;

        public string Bio { get; set; } =
            string.Empty;

        public string Country { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string? ProfileImageUrl { get; set; }
    }
}