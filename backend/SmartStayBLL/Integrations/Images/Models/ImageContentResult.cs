namespace SmartStayBLL
{
    public sealed class ImageContentResult
    {
        public byte[] Content { get; set; } =
            Array.Empty<byte>();

        public string ContentType { get; set; } =
            "application/octet-stream";
    }
}