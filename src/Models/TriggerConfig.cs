using System;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Configures execution timeline rules and behavior boundary criteria for task processing structures.
    /// </summary>
    public class TriggerConfig
    {
        /// <summary>
        /// Gets or sets the category mechanism defining validation metrics (e.g., daily, startup).
        /// </summary>
        [YamlMember(Alias = "type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this monitoring configuration is enabled.
        /// </summary>
        [YamlMember(Alias = "enabled")]
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the historical starting baseline timestamp enabling engine execution operations.
        /// </summary>
        [YamlMember(Alias = "startBoundary")]
        [JsonPropertyName("startBoundary")]
        public DateTime? StartBoundary { get; set; }

        /// <summary>
        /// Gets or sets the critical threshold date cutoff limiting scheduler task engine actions.
        /// </summary>
        [YamlMember(Alias = "endBoundary")]
        [JsonPropertyName("endBoundary")]
        public DateTime? EndBoundary { get; set; }

        /// <summary>
        /// Gets or sets the total allowed lifespan runtime limits constraint for target processing.
        /// </summary>
        [YamlMember(Alias = "executionTimeLimit")]
        [JsonPropertyName("executionTimeLimit")]
        public TimeSpan? ExecutionTimeLimit { get; set; }

        /// <summary>
        /// Gets or sets an optional semantic structural key name identifying the tracking element trigger.
        /// </summary>
        [YamlMember(Alias = "id")]
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets specialized loop configurations specifying fine-grained automation patterns.
        /// </summary>
        [YamlMember(Alias = "repetition")]
        [JsonPropertyName("repetition")]
        public RepetitionPatternConfig? Repetition { get; set; }

        /// <summary>
        /// Gets or sets a predefined offset wait parameter inserted prior to processing instructions.
        /// </summary>
        [YamlMember(Alias = "delay")]
        [JsonPropertyName("delay")]
        public TimeSpan? Delay { get; set; }
    }
}