namespace SmartStayBLL
{
    public sealed class ImageUploadResult
    {
        public string SecureUrl { get; init; } = string.Empty;

        public string PublicId { get; init; } = string.Empty;

        public string Format { get; init; } = string.Empty;

        public long FileSizeInBytes { get; init; }

        public int Width { get; init; }

        public int Height { get; init; }
    }
}