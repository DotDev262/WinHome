using YamlDotNet.Serialization;

namespace WinHome.Models.Plugins
{
    /// <summary>
    /// Defines the metadata and configuration manifest parameters for an external plugin.
    /// </summary>
    public class PluginManifest
    {
        /// <summary>
        /// Gets or sets the unique display name of the plugin.
        /// </summary>
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the semantic version string of the plugin.
        /// </summary>
        [YamlMember(Alias = "version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the execution environment or language type used to run the plugin.
        /// </summary>
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = "executable"; // python, typescript, executable

        /// <summary>
        /// Gets or sets the primary entry-point script or executable file path for the plugin.
        /// </summary>
        [YamlMember(Alias = "main")]
        public string Main { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of operational capabilities or permissions requested by the plugin.
        /// </summary>
        [YamlMember(Alias = "capabilities")]
        public List<string> Capabilities { get; set; } = new();

        /// <summary>
        /// Gets or sets the internal local directory path established during plugin discovery.
        /// </summary>
        // Internal path set during discovery
        public string DirectoryPath { get; set; } = string.Empty;
    }
}