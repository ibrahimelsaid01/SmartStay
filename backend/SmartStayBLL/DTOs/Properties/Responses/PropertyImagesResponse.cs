namespace SmartStayBLL
{
    public sealed class PropertyImagesResponse
    {
        public Guid PropertyId { get; set; }

        public int ImagesCount { get; set; }

        public Guid? CoverImageId { get; set; }

        public IReadOnlyList<PropertyImageResponse>
            Images
        { get; set; } =
                Array.Empty<PropertyImageResponse>();

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}