using System;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class RepetitionPatternConfig
    {
        [YamlMember(Alias = "interval")]
        [JsonPropertyName("interval")]
        public TimeSpan Interval { get; set; }

        [YamlMember(Alias = "duration")]
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }

        [YamlMember(Alias = "stopAtDurationEnd")]
        [JsonPropertyName("stopAtDurationEnd")]
        public bool StopAtDurationEnd { get; set; }
    }
}
