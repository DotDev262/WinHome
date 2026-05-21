using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Describes environment variable adjustments applied globally or dynamically within sessions.
    /// </summary>
    public class EnvVarConfig
    {
        /// <summary>
        /// Gets or sets the key identifier/name of the target environment variable.
        /// </summary>
        [YamlMember(Alias = "variable")]
        [JsonPropertyName("variable")]
        public string Variable { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing value assigned to the environment variable path.
        /// </summary>
        [YamlMember(Alias = "value")]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the specific modifier behavior action. Defaults to "set".
        /// </summary>
        [YamlMember(Alias = "action")]
        [JsonPropertyName("action")]
        public string Action { get; set; } = "set";
    }
}