using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Describes structural parameter management objectives relating directly to host Windows background service utilities.
    /// </summary>
    public class WindowsServiceConfig
    {
        /// <summary>
        /// Gets or sets the backend identifier key name mapping the host instance application tool.
        /// </summary>
        [YamlMember(Alias = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets target background execution states (e.g., running, stopped). Defaults to "running".
        /// </summary>
        [YamlMember(Alias = "state")]
        [JsonPropertyName("state")]
        public string State { get; set; } = "running";

        /// <summary>
        /// Gets or sets operating parameters controlling initialization types (e.g., automatic, manual, disabled).
        /// </summary>
        [YamlMember(Alias = "startup")]
        [JsonPropertyName("startup")]
        public string? StartupType { get; set; }
    }
}