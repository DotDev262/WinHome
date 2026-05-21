using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Contains sub-configurations specific to designated workspace profiles.
    /// </summary>
    public class ProfileConfig
    {
        /// <summary>
        /// Gets or sets the profile-specific Git infrastructure parameters.
        /// </summary>
        [YamlMember(Alias = "git")]
        [JsonPropertyName("git")]
        public GitConfig? Git { get; set; }
    }
}