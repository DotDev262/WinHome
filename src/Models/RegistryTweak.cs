using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Instructs atomic manipulations onto specific fields inside the target OS system database registry.
    /// </summary>
    public class RegistryTweak
    {
        /// <summary>
        /// Gets or sets the registry hive folder directory navigation path.
        /// </summary>
        [YamlMember(Alias = "path")]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the explicit name mapping element key.
        /// </summary>
        [YamlMember(Alias = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data storage component value.
        /// </summary>
        [YamlMember(Alias = "value")]
        [JsonPropertyName("value")]
        public object Value { get; set; } = new();

        /// <summary>
        /// Gets or sets the specific data representation primitive format type. Defaults to "string".
        /// </summary>
        [YamlMember(Alias = "type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";
    }
}