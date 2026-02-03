using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class ScheduledTaskConfig
    {
        [YamlMember(Alias = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "path")]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "description")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [YamlMember(Alias = "author")]
        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [YamlMember(Alias = "triggers")]
        [JsonPropertyName("triggers")]
        public List<TriggerConfig> Triggers { get; set; } = new();

        [YamlMember(Alias = "actions")]
        [JsonPropertyName("actions")]
        public List<ActionConfig> Actions { get; set; } = new();
    }
}
