using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinHome.Services.System
{
  public class ConfigDriftService : IConfigDriftService
  {
    private readonly IDeserializer _deserializer;

    public ConfigDriftService()
    {
      _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    }

    public async Task<List<ConfigDriftResult>> DetectDriftAsync(string backupFile)
    {
      var drifts = new List<ConfigDriftResult>();

      if (!File.Exists(backupFile))
      {
        throw new FileNotFoundException("Backup file not found.", backupFile);
      }

      var backupYaml = await File.ReadAllTextAsync(backupFile);

      var backup =
        _deserializer.Deserialize<ConfigBackupModel>(backupYaml);

      if (backup == null)
      {
        return drifts;
      }

      if (!File.Exists("config.yaml"))
      {
        throw new FileNotFoundException("config.yaml not found.");
      }

      var configYaml = await File.ReadAllTextAsync("config.yaml");

      var config =
        _deserializer.Deserialize<Configuration>(configYaml);

      if (config == null)
      {
        return drifts;
      }

      if (!config.Extensions.TryGetValue(
            backup.Provider,
            out var currentSettings))
      {
        drifts.Add(new ConfigDriftResult
        {
          Provider = backup.Provider,
          Key = "__provider__",
          Expected = "Present",
          Actual = "Missing"
        });

        return drifts;
      }

      string expectedJson =
        JsonSerializer.Serialize(backup.Settings);

      string actualJson =
        JsonSerializer.Serialize(currentSettings);

      if (expectedJson != actualJson)
      {
        drifts.Add(new ConfigDriftResult
        {
          Provider = backup.Provider,
          Key = "settings",
          Expected = expectedJson,
          Actual = actualJson
        });
      }

      return drifts;
    }

    private class ConfigBackupModel
    {
      public string Provider { get; set; } = "";
      public string Version { get; set; } = "";
      public string SourcePath { get; set; } = "";
      public DateTime CreatedAt { get; set; }
      public object? Settings { get; set; }
    }
  }
}
