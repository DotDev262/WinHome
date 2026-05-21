using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Governs core infrastructure rules applied toward standard local Windows Subsystem for Linux subsystems.
    /// </summary>
    public class WslConfig
    {
        /// <summary>
        /// Gets or sets the universal runtime major edition type. Defaults to 2.
        /// </summary>
        [YamlMember(Alias = "defaultVersion")]
        [JsonPropertyName("defaultVersion")]
        public int DefaultVersion { get; set; } = 2;

        /// <summary>
        /// Gets or sets the custom preferred distro ecosystem selected for launching interactive tasks.
        /// </summary>
        [YamlMember(Alias = "defaultDistro")]
        [JsonPropertyName("defaultDistro")]
        public string? DefaultDistro { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether automated routine check patches are executed to update tracking distros.
        /// </summary>
        [YamlMember(Alias = "update")]
        [JsonPropertyName("update")]
        public bool Update { get; set; } = false;

        /// <summary>
        /// Gets or sets the sequence arrays hosting configuration details for unique provisioned guest installations.
        /// </summary>
        [YamlMember(Alias = "distros")]
        [JsonPropertyName("distros")]
        public List<WslDistroConfig> Distros { get; set; } = new();
    }
}