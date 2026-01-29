using System;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class RepetitionPatternConfig
    {
        [YamlMember(Alias = "interval")]
        public TimeSpan Interval { get; set; }

        [YamlMember(Alias = "duration")]
        public TimeSpan Duration { get; set; }

        [YamlMember(Alias = "stopAtDurationEnd")]
        public bool StopAtDurationEnd { get; set; }
    }
}
