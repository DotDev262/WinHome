using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Configures mappings to link source configuration dotfiles into active workspace destinations.
    /// </summary>
    public class DotfileConfig
    {
        /// <summary>
        /// Gets or sets the path to the origin repository dotfile asset.
        /// </summary>
        [YamlMember(Alias = "src")]
        [JsonPropertyName("src")]
        public string Src { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target system environment path where the file will be mapped.
        /// </summary>
        [YamlMember(Alias = "target")]
        [JsonPropertyName("target")]
        public string Target { get; set; } = string.Empty;
    }
}