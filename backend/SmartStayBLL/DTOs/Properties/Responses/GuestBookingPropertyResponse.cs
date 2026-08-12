namespace SmartStayBLL
{
    public sealed class GuestBookingPropertyResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string Country { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string? CoverImageUrl { get; set; }
    }
}