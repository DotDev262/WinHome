using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class AppConfig
    {
        [YamlMember(Alias = "id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [YamlMember(Alias = "source")]
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [YamlMember(Alias = "manager")]
        [JsonPropertyName("manager")]
        public string Manager { get; set; } = "winget";

        [YamlMember(Alias = "version")]
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [YamlMember(Alias = "params")]
        [JsonPropertyName("params")]
        public string? Params { get; set; }
    }
}
