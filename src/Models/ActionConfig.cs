using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Represents the configuration settings for executing an action or process.
    /// </summary>
    public class ActionConfig
    {
        /// <summary>
        /// Gets or sets the type of action execution (e.g., executable, script).
        /// </summary>
        [YamlMember(Alias = "type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path or command to be executed.
        /// </summary>
        [YamlMember(Alias = "path")]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional command-line arguments to pass to the action.
        /// </summary>
        [YamlMember(Alias = "arguments")]
        [JsonPropertyName("arguments")]
        public string? Arguments { get; set; }

        /// <summary>
        /// Gets or sets the optional working directory path where the action should execute.
        /// </summary>
        [YamlMember(Alias = "workingDirectory")]
        [JsonPropertyName("workingDirectory")]
        public string? WorkingDirectory { get; set; }
    }
}