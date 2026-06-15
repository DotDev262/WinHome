using System.Text.Json;
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

  public async Task BackupAsync(Configuration config, string output)
  {
    var backup = new
    {
      provider = "winhome",
      version = config.Version,
      createdAt = DateTime.UtcNow,
      configuration = config
    };

    var yaml = _serializer.Serialize(backup);

    var tmp = $"{output}.tmp";

    await File.WriteAllTextAsync(tmp, yaml);

    File.Move(
        tmp,
        output,
        true);
  }

  public async Task<Configuration> RestoreAsync(string input)
  {
    if (!File.Exists(input))
      throw new FileNotFoundException("Backup file not found.", input);

    var content = await File.ReadAllTextAsync(input);

    var backup = _deserializer.Deserialize<ConfigBackupModel>(content);

    if (backup?.Configuration == null)
      throw new InvalidDataException("Invalid WinHome backup format.");

    return backup.Configuration;
  }

  private class ConfigBackupModel
  {
    public string Provider { get; set; } = "";
    public string Version { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Configuration? Configuration { get; set; }
  }
}
