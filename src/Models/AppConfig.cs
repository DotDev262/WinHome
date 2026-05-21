using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Represents the configuration for a software application installation or management profile.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Gets or sets the unique identifier or name of the application package.
        /// </summary>
        [YamlMember(Alias = "id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the custom installation source URL or repository channel.
        /// </summary>
        [YamlMember(Alias = "source")]
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the package manager responsible for handling the app. Defaults to "winget".
        /// </summary>
        [YamlMember(Alias = "manager")]
        [JsonPropertyName("manager")]
        public string Manager { get; set; } = "winget";

        /// <summary>
        /// Gets or sets the specific target version of the application to deploy.
        /// </summary>
        [YamlMember(Alias = "version")]
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets additional installer parameters or arguments.
        /// </summary>
        [YamlMember(Alias = "params")]
        [JsonPropertyName("params")]
        public string? Params { get; set; }
    }
}