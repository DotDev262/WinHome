using System.Text.Json.Serialization;

namespace WinHome.Models.Plugins
{
    public class PluginRequest
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("args")]
        public object? Args { get; set; }

        [JsonPropertyName("context")]
        public object? Context { get; set; }
    }

    public class PluginResult
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("changed")]
        public bool Changed { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}
