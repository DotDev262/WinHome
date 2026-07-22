using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WinHome.Models.Plugins
{
  /// <summary>Defines installation package sources for auto-installing the plugin's prerequisite application.</summary>
  public class PluginInstallInfo
  {
    [YamlMember(Alias = "default_manager")]
    public string DefaultManager { get; set; } = string.Empty;

    [YamlMember(Alias = "packages")]
    public Dictionary<string, string> Packages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
  }
}
