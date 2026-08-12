using System.Text.Json.Serialization;

namespace SmartStayBLL
{
    public sealed class FacebookDebugTokenResponse
    {
        [JsonPropertyName("data")]
        public FacebookDebugTokenData? Data { get; set; }
    }

    public sealed class FacebookDebugTokenData
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; set; } = string.Empty;

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; set; }

        [JsonPropertyName("data_access_expires_at")]
        public long DataAccessExpiresAt { get; set; }

        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; } = [];
    }
}