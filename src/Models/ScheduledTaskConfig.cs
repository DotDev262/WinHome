using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class ScheduledTaskConfig
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        [YamlMember(Alias = "author")]
        public string? Author { get; set; }

        [YamlMember(Alias = "triggers")]
        public List<TriggerConfig> Triggers { get; set; } = new();

        [YamlMember(Alias = "actions")]
        public List<ActionConfig> Actions { get; set; } = new();
    }
}
