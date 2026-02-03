using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class WslConfig
    {
        [YamlMember(Alias = "defaultVersion")]
        [JsonPropertyName("defaultVersion")]
        public int DefaultVersion { get; set; } = 2;

        [YamlMember(Alias = "defaultDistro")]
        [JsonPropertyName("defaultDistro")]
        public string? DefaultDistro { get; set; }

        [YamlMember(Alias = "update")]
        [JsonPropertyName("update")]
        public bool Update { get; set; } = false;

        
        [YamlMember(Alias = "distros")]
        [JsonPropertyName("distros")]
        public List<WslDistroConfig> Distros { get; set; } = new();
    }
}
