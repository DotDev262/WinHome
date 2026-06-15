using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
  /// <summary>Complete configuration for a Windows Scheduled Task including triggers and actions.</summary>
  public class ScheduledTaskConfig : ResourceBase
  {
    /// <summary>Registered task name in Task Scheduler. Combined with <see cref="Path"/> to form the full registration path.</summary>
    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional Task Scheduler folder path (e.g., "\WinHome"). Combined with <see cref="Name"/> to form the full registration path. Leave empty to register at the root folder.</summary>
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
