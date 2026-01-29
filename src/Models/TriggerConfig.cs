using System;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class TriggerConfig
    {
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = string.Empty;

        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; } = true;

        [YamlMember(Alias = "startBoundary")]
        public DateTime? StartBoundary { get; set; }

        [YamlMember(Alias = "endBoundary")]
        public DateTime? EndBoundary { get; set; }

        [YamlMember(Alias = "executionTimeLimit")]
        public TimeSpan? ExecutionTimeLimit { get; set; }

        [YamlMember(Alias = "id")]
        public string? Id { get; set; }

        [YamlMember(Alias = "repetition")]
        public RepetitionPatternConfig? Repetition { get; set; }

        [YamlMember(Alias = "delay")]
        public TimeSpan? Delay { get; set; }
    }
}
