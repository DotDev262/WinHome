using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class ActionConfig
    {
        [YamlMember(Alias = "type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [YamlMember(Alias = "path")]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "arguments")]
        [JsonPropertyName("arguments")]
        public string? Arguments { get; set; }

        [YamlMember(Alias = "workingDirectory")]
        [JsonPropertyName("workingDirectory")]
        public string? WorkingDirectory { get; set; }
    }
}
