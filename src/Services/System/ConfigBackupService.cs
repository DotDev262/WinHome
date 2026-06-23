using WinHome.Interfaces;
using WinHome.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinHome.Services.System;

public class ConfigBackupService : IConfigBackupService
{
  private readonly ISerializer _serializer;
  private readonly IDeserializer _deserializer;

  public ConfigBackupService()
  {
    _serializer = new SerializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
      .Build();

    _deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();
  }

  public async Task BackupAsync(
      string provider,
      Configuration config,
      string sourcePath,
      string output)
  {
    if (!config.Extensions.TryGetValue(provider, out var settings))
    {
      throw new InvalidOperationException(
          $"Provider '{provider}' not found in configuration.");
    }

    var backup = new ConfigBackupModel
    {
      Provider = provider,
      Version = config.Version,
      SourcePath = sourcePath,
      CreatedAt = DateTime.UtcNow,
      Settings = settings
    };

    var yaml = _serializer.Serialize(backup);

    var tmp = $"{output}.tmp";

    await File.WriteAllTextAsync(tmp, yaml);

    File.Move(
      tmp,
      output,
      true);
  }

  public async Task<(string Provider, object? Settings)> RestoreAsync(
      string input)
  {
    if (!File.Exists(input))
    {
      throw new FileNotFoundException(
          "Backup file not found.",
          input);
    }

    var content = await File.ReadAllTextAsync(input);

    var backup = _deserializer.Deserialize<ConfigBackupModel>(content);

    if (backup == null)
    {
      throw new InvalidDataException(
          "Invalid WinHome backup format.");
    }

    return (
        backup.Provider,
        backup.Settings);
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
