using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Identifies tracking settings binding instance identities for single Linux workspace nodes.
    /// </summary>
    public class WslDistroConfig
    {
        /// <summary>
        /// Gets or sets the target instance mapping label corresponding to environmental distribution signatures.
        /// </summary>
        [YamlMember(Alias = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional file pointer target specifying setup logic initialization scripts.
        /// </summary>
        [YamlMember(Alias = "setupScript")]
        [JsonPropertyName("setupScript")]
        public string? SetupScript { get; set; }
    }
}