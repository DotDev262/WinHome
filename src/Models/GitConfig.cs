using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
     public class GitConfig
    {
        // Convenience properties (Common stuff)
        [YamlMember(Alias = "userName")]
        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [YamlMember(Alias = "userEmail")]
        [JsonPropertyName("userEmail")]
        public string? UserEmail { get; set; }

        [YamlMember(Alias = "signingKey")]
        [JsonPropertyName("signingKey")]
        public string? SigningKey { get; set; }

        [YamlMember(Alias = "commitGpgSign")]
        [JsonPropertyName("commitGpgSign")]
        public bool? CommitGpgSign { get; set; }

        // NEW: Generic Dictionary for EVERYTHING else
        // Maps "core.editor" -> "code --wait"
        [YamlMember(Alias = "settings")]
        [JsonPropertyName("settings")]
        public Dictionary<string, string> Settings { get; set; } = new();
    }
}
