using System.Text.Json.Serialization;

namespace SmartStayBLL
{
    public sealed class FacebookUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; set; }
    }

    public sealed class FacebookPicture
    {
        [JsonPropertyName("data")]
        public FacebookPictureData? Data { get; set; }
    }

    public sealed class FacebookPictureData
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("is_silhouette")]
        public bool IsSilhouette { get; set; }
    }
}