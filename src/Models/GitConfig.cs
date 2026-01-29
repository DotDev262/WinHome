using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
     public class GitConfig
    {
        // Convenience properties (Common stuff)
        [YamlMember(Alias = "userName")]
        public string? UserName { get; set; }

        [YamlMember(Alias = "userEmail")]
        public string? UserEmail { get; set; }

        [YamlMember(Alias = "signingKey")]
        public string? SigningKey { get; set; }

        [YamlMember(Alias = "commitGpgSign")]
        public bool? CommitGpgSign { get; set; }

        // NEW: Generic Dictionary for EVERYTHING else
        // Maps "core.editor" -> "code --wait"
        [YamlMember(Alias = "settings")]
        public Dictionary<string, string> Settings { get; set; } = new();
    }
}
