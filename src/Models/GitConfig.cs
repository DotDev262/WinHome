using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Configures Git identity presets, keys, and extended settings blocks.
    /// </summary>
    public class GitConfig
    {
        /// <summary>
        /// Gets or sets the globally applied profile display username.
        /// </summary>
        [YamlMember(Alias = "userName")]
        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the communication contact electronic email associated with commits.
        /// </summary>
        [YamlMember(Alias = "userEmail")]
        [JsonPropertyName("userEmail")]
        public string? UserEmail { get; set; }

        /// <summary>
        /// Gets or sets the asymmetric encryption signature signing token.
        /// </summary>
        [YamlMember(Alias = "signingKey")]
        [JsonPropertyName("signingKey")]
        public string? SigningKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether standard commits force cryptographical verification checks.
        /// </summary>
        [YamlMember(Alias = "commitGpgSign")]
        [JsonPropertyName("commitGpgSign")]
        public bool? CommitGpgSign { get; set; }

        /// <summary>
        /// Gets or sets an extensible generic settings collection that maps configuration paths directly to runtime strings.
        /// </summary>
        [YamlMember(Alias = "settings")]
        [JsonPropertyName("settings")]
        public Dictionary<string, string> Settings { get; set; } = new();
    }
}