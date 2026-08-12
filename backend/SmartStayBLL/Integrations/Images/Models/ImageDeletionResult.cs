namespace SmartStayBLL
{
    public sealed class ImageDeletionResult
    {
        public bool IsDeleted { get; init; }

        public string PublicId { get; init; } = string.Empty;

        public string? ProviderResult { get; init; }
    }
}