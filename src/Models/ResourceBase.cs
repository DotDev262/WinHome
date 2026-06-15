using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Base class for all resource config types that participate in the
    /// dependency graph. Provides optional <see cref="ResourceId"/> and
    /// <see cref="DependsOn"/> fields without touching existing functional
    /// properties (e.g. AppConfig.Id which is the package identifier).
    /// </summary>
    public abstract class ResourceBase
    {
        /// <summary>
        /// Optional unique identifier for this resource within the config file.
        /// Used by other resources to reference this one in <see cref="DependsOn"/>.
        /// Must be unique across ALL resources in the config if specified.
        /// </summary>
        [YamlMember(Alias = "resourceId")]
        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; set; }

        /// <summary>
        /// Optional list of <see cref="ResourceId"/> values that must be fully
        /// applied before this resource is processed.
        /// </summary>
        [YamlMember(Alias = "dependsOn")]
        [JsonPropertyName("dependsOn")]
        public List<string>? DependsOn { get; set; }
    }
}