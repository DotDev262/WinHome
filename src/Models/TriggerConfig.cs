using System;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class TriggerConfig
    {
        [YamlMember(Alias = "type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [YamlMember(Alias = "enabled")]
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [YamlMember(Alias = "startBoundary")]
        [JsonPropertyName("startBoundary")]
        public DateTime? StartBoundary { get; set; }

        [YamlMember(Alias = "endBoundary")]
        [JsonPropertyName("endBoundary")]
        public DateTime? EndBoundary { get; set; }

        [YamlMember(Alias = "executionTimeLimit")]
        [JsonPropertyName("executionTimeLimit")]
        public TimeSpan? ExecutionTimeLimit { get; set; }

        [YamlMember(Alias = "id")]
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [YamlMember(Alias = "repetition")]
        [JsonPropertyName("repetition")]
        public RepetitionPatternConfig? Repetition { get; set; }

        [YamlMember(Alias = "delay")]
        [JsonPropertyName("delay")]
        public TimeSpan? Delay { get; set; }
    }
}
