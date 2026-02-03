using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class WindowsServiceConfig
    {
        [YamlMember(Alias = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "state")]
        [JsonPropertyName("state")]
        public string State { get; set; } = "running"; // running, stopped

        [YamlMember(Alias = "startup")]
        [JsonPropertyName("startup")]
        public string? StartupType { get; set; } // automatic, manual, disabled
    }
}
