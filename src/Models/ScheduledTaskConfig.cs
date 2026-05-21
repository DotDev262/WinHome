using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Presets local task configurations targeting integration deployment parameters into system scheduler queues.
    /// </summary>
    public class ScheduledTaskConfig
    {
        /// <summary>
        /// Gets or sets the system-registered unique lookup execution task identifier.
        /// </summary>
        [YamlMember(Alias = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the localized tracking organizational pathway index folder location.
        /// </summary>
        [YamlMember(Alias = "path")]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a helpful overview explaining the programmatic intention behind the scheduled task.
        /// </summary>
        [YamlMember(Alias = "description")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets metadata identifying the developer or workflow process that declared the task object.
        /// </summary>
        [YamlMember(Alias = "author")]
        [JsonPropertyName("author")]
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the structural array of conditional criteria rules initializing pipeline tasks.
        /// </summary>
        [YamlMember(Alias = "triggers")]
        [JsonPropertyName("triggers")]
        public List<TriggerConfig> Triggers { get; set; } = new();

        /// <summary>
        /// Gets or sets sequential programmatic instructions to perform sequentially when triggered.
        /// </summary>
        [YamlMember(Alias = "actions")]
        [JsonPropertyName("actions")]
        public List<ActionConfig> Actions { get; set; } = new();
    }
}