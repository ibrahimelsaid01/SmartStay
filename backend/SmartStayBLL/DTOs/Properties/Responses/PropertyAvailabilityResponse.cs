namespace SmartStayBLL
{
    public sealed class PropertyAvailabilityResponse
    {
        public Guid PropertyId { get; set; }

        public DateOnly CheckInDate { get; set; }

        public DateOnly CheckOutDate { get; set; }

        public int GuestsCount { get; set; }

        public int Nights { get; set; }

        public bool IsAvailable { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}