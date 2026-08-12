namespace SmartStayBLL
{
    public sealed class PropertyAmenitiesResponse
    {
        public Guid PropertyId { get; set; }

        public int SelectedAmenitiesCount { get; set; }

        public IReadOnlyList<AmenityResponse>
            Amenities
        { get; set; } =
                Array.Empty<AmenityResponse>();

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}