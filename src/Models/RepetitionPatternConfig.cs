using System;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Controls chronological repetition intervals and limitations during event scheduling triggers.
    /// </summary>
    public class RepetitionPatternConfig
    {
        /// <summary>
        /// Gets or sets the recurring delay length between task invocations.
        /// </summary>
        [YamlMember(Alias = "interval")]
        [JsonPropertyName("interval")]
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets or sets the comprehensive length of time the repetition remains active.
        /// </summary>
        [YamlMember(Alias = "duration")]
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether runtime triggers stop completely after the duration timeline is surpassed.
        /// </summary>
        [YamlMember(Alias = "stopAtDurationEnd")]
        [JsonPropertyName("stopAtDurationEnd")]
        public bool StopAtDurationEnd { get; set; }
    }
}