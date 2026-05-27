using System.Text.Json;
using YamlDotNet.Serialization;
using WinHome.Models;
using WinHome.Models.Plugins;

namespace WinHome.Tests;

public class ModelTests
{
  #region AppConfig Tests

  [Fact]
  public void AppConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new AppConfig();

    // Assert
    Assert.Equal(string.Empty, config.Id);
    Assert.Null(config.Source);
    Assert.Equal("winget", config.Manager);
    Assert.Null(config.Version);
    Assert.Null(config.Params);
  }

  [Fact]
  public void AppConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new AppConfig
    {
      Id = "Microsoft.PowerToys",
      Source = "winget",
      Manager = "winget",
      Version = "0.80.0",
      Params = "--silent --force"
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<AppConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Id, deserialized.Id);
    Assert.Equal(original.Source, deserialized.Source);
    Assert.Equal(original.Manager, deserialized.Manager);
    Assert.Equal(original.Version, deserialized.Version);
    Assert.Equal(original.Params, deserialized.Params);
  }

  [Fact]
  public void AppConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new AppConfig
    {
      Id = "neovim",
      Source = "scoop",
      Manager = "scoop",
      Version = "0.10.0",
      Params = "--global"
    };

    // Act
    var serializer = new SerializerBuilder().Build();
    var yamlString = serializer.Serialize(original);
    var deserializer = new DeserializerBuilder().Build();
    var deserialized = deserializer.Deserialize<AppConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Id, deserialized.Id);
    Assert.Equal(original.Source, deserialized.Source);
    Assert.Equal(original.Manager, deserialized.Manager);
    Assert.Equal(original.Version, deserialized.Version);
    Assert.Equal(original.Params, deserialized.Params);
  }

  #endregion

  #region PluginManifest Tests

  [Fact]
  public void PluginManifest_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var manifest = new PluginManifest();

    // Assert
    Assert.Equal(string.Empty, manifest.Name);
    Assert.Equal("1.0.0", manifest.Version);
    Assert.Equal("executable", manifest.Type);
    Assert.Equal(string.Empty, manifest.Main);
    Assert.NotNull(manifest.Capabilities);
    Assert.Empty(manifest.Capabilities);
    Assert.Equal(string.Empty, manifest.DirectoryPath);
  }

  [Fact]
  public void PluginManifest_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new PluginManifest
    {
      Name = "sample-plugin",
      Version = "2.0.0",
      Type = "typescript",
      Main = "dist/index.js",
      Capabilities = new List<string> { "config", "validate" },
      DirectoryPath = "/plugins/sample"
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);

    // Assert serialized contract (YAML keys and list values)
    Assert.Contains("name: sample-plugin", yamlString);
    Assert.Contains("version: 2.0.0", yamlString);
    Assert.Contains("type: typescript", yamlString);
    Assert.Contains("main: dist/index.js", yamlString);
    Assert.Contains("capabilities:", yamlString);
    Assert.Contains("- config", yamlString);
    Assert.Contains("- validate", yamlString);

    var deserialized = deserializer.Deserialize<PluginManifest>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.Version, deserialized.Version);
    Assert.Equal(original.Type, deserialized.Type);
    Assert.Equal(original.Main, deserialized.Main);
    Assert.Equal(original.Capabilities, deserialized.Capabilities);
    Assert.Equal(original.DirectoryPath, deserialized.DirectoryPath);
  }

  [Fact]
  public void PluginManifest_ShouldDeserialize_FromPartialYaml()
  {
    // Arrange
    var yaml = @"name: test-plugin
main: src/plugin.py
type: python
capabilities:
  - config_provider
";

    var deserializer = new DeserializerBuilder().Build();

    // Act
    var manifest = deserializer.Deserialize<PluginManifest>(yaml);

    // Assert
    Assert.NotNull(manifest);
    Assert.Equal("test-plugin", manifest.Name);
    Assert.Equal("src/plugin.py", manifest.Main);
    Assert.Equal("python", manifest.Type);
    Assert.Equal("1.0.0", manifest.Version);
    Assert.Equal(string.Empty, manifest.DirectoryPath);
    Assert.Single(manifest.Capabilities);
    Assert.Equal("config_provider", manifest.Capabilities[0]);
  }

  #endregion

  #region PluginRequest Tests

  [Fact]
  public void PluginRequest_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var request = new PluginRequest();

    // Assert
    Assert.False(string.IsNullOrWhiteSpace(request.RequestId));
    Assert.Equal(string.Empty, request.Command);
    Assert.Null(request.Args);
    Assert.Null(request.Context);
  }

  [Fact]
  public void PluginRequest_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new PluginRequest
    {
      RequestId = "req-123",
      Command = "apply",
      Args = new { name = "demo" },
      Context = new { profile = "default" }
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);

    // Assert serialized contract (JSON property casing)
    using var jsonDoc = JsonDocument.Parse(jsonString);
    var root = jsonDoc.RootElement;
    Assert.True(root.TryGetProperty("requestId", out var requestIdElement));
    Assert.Equal("req-123", requestIdElement.GetString());
    Assert.True(root.TryGetProperty("command", out var commandElement));
    Assert.Equal("apply", commandElement.GetString());
    Assert.True(root.TryGetProperty("args", out var argsElement));
    Assert.Equal(JsonValueKind.Object, argsElement.ValueKind);
    Assert.True(root.TryGetProperty("context", out var contextElement));
    Assert.Equal(JsonValueKind.Object, contextElement.ValueKind);

    var deserialized = JsonSerializer.Deserialize<PluginRequest>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.RequestId, deserialized.RequestId);
    Assert.Equal(original.Command, deserialized.Command);

    var deserializedArgs = Assert.IsType<JsonElement>(deserialized.Args);
    Assert.Equal("demo", deserializedArgs.GetProperty("name").GetString());

    var deserializedContext = Assert.IsType<JsonElement>(deserialized.Context);
    Assert.Equal("default", deserializedContext.GetProperty("profile").GetString());
  }

  [Theory]
  [InlineData("{\"args\": [\"a\", \"b\"]}", JsonValueKind.Array)]
  [InlineData("{\"args\": \"hello\"}", JsonValueKind.String)]
  [InlineData("{\"args\": 42}", JsonValueKind.Number)]
  [InlineData("{\"args\": null}", JsonValueKind.Null)]
  public void PluginRequest_ShouldDeserialize_WithVariousArgTypes(string json, JsonValueKind expectedKind)
  {
    // Act
    var request = JsonSerializer.Deserialize<PluginRequest>(json);

    // Assert
    Assert.NotNull(request);

    if (expectedKind == JsonValueKind.Null)
    {
      Assert.Null(request.Args);
      return;
    }

    var argsElement = Assert.IsType<JsonElement>(request.Args);
    Assert.Equal(expectedKind, argsElement.ValueKind);
  }

  #endregion

  #region PluginResult Tests

  [Fact]
  public void PluginResult_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var result = new PluginResult();

    // Assert
    Assert.Equal(string.Empty, result.RequestId);
    Assert.False(result.Success);
    Assert.False(result.Changed);
    Assert.Null(result.Error);
    Assert.Null(result.Data);
  }

  [Fact]
  public void PluginResult_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new PluginResult
    {
      RequestId = "req-123",
      Success = true,
      Changed = true,
      Error = null,
      Data = new { message = "ok" }
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);

    // Assert serialized contract (JSON property casing)
    using var jsonDoc = JsonDocument.Parse(jsonString);
    var root = jsonDoc.RootElement;
    Assert.True(root.TryGetProperty("requestId", out var requestIdElement));
    Assert.Equal("req-123", requestIdElement.GetString());
    Assert.True(root.TryGetProperty("success", out var successElement));
    Assert.True(successElement.GetBoolean());
    Assert.True(root.TryGetProperty("changed", out var changedElement));
    Assert.True(changedElement.GetBoolean());
    Assert.True(root.TryGetProperty("error", out var errorElement));
    Assert.Equal(JsonValueKind.Null, errorElement.ValueKind);
    Assert.True(root.TryGetProperty("data", out var dataElement));
    Assert.Equal(JsonValueKind.Object, dataElement.ValueKind);

    var deserialized = JsonSerializer.Deserialize<PluginResult>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.RequestId, deserialized.RequestId);
    Assert.Equal(original.Success, deserialized.Success);
    Assert.Equal(original.Changed, deserialized.Changed);
    Assert.Null(deserialized.Error);

    var deserializedData = Assert.IsType<JsonElement>(deserialized.Data);
    Assert.Equal("ok", deserializedData.GetProperty("message").GetString());
  }

  [Fact]
  public void PluginResult_ShouldSerialize_WithErrorMessage()
  {
    // Arrange
    var result = new PluginResult
    {
      RequestId = "req-err",
      Success = false,
      Changed = false,
      Error = "Something went wrong"
    };

    // Act
    var json = JsonSerializer.Serialize(result);

    // Assert
    using var doc = JsonDocument.Parse(json);
    Assert.Equal("Something went wrong", doc.RootElement.GetProperty("error").GetString());
  }

  [Fact]
  public void PluginResult_ShouldDeserialize_MissingOptionalFields()
  {
    // Arrange
    var json = "{\"requestId\":\"r1\",\"success\":true,\"changed\":true}";

    // Act
    var result = JsonSerializer.Deserialize<PluginResult>(json);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("r1", result.RequestId);
    Assert.True(result.Success);
    Assert.True(result.Changed);
    Assert.Null(result.Error);
    Assert.Null(result.Data);
  }

  #endregion

  #region ActionConfig Tests

  [Fact]
  public void ActionConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new ActionConfig();

    // Assert
    Assert.Equal(string.Empty, config.Type);
    Assert.Equal(string.Empty, config.Path);
    Assert.Null(config.Arguments);
    Assert.Null(config.WorkingDirectory);
  }

  [Fact]
  public void ActionConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new ActionConfig
    {
      Type = "exec",
      Path = "C:\\Tools\\run.ps1",
      Arguments = "-Verbose",
      WorkingDirectory = "C:\\Tools"
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<ActionConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Type, deserialized.Type);
    Assert.Equal(original.Path, deserialized.Path);
    Assert.Equal(original.Arguments, deserialized.Arguments);
    Assert.Equal(original.WorkingDirectory, deserialized.WorkingDirectory);
  }

  [Fact]
  public void ActionConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new ActionConfig
    {
      Type = "exec",
      Path = "/usr/bin/echo",
      Arguments = "hello",
      WorkingDirectory = "/tmp"
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<ActionConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Type, deserialized.Type);
    Assert.Equal(original.Path, deserialized.Path);
    Assert.Equal(original.Arguments, deserialized.Arguments);
    Assert.Equal(original.WorkingDirectory, deserialized.WorkingDirectory);
  }

  #endregion

  #region RepetitionPatternConfig Tests

  [Fact]
  public void RepetitionPatternConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new RepetitionPatternConfig();

    // Assert
    Assert.Equal(TimeSpan.Zero, config.Interval);
    Assert.Equal(TimeSpan.Zero, config.Duration);
    Assert.False(config.StopAtDurationEnd);
  }

  [Fact]
  public void RepetitionPatternConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new RepetitionPatternConfig
    {
      Interval = TimeSpan.FromMinutes(10),
      Duration = TimeSpan.FromHours(2),
      StopAtDurationEnd = true
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<RepetitionPatternConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Interval, deserialized.Interval);
    Assert.Equal(original.Duration, deserialized.Duration);
    Assert.Equal(original.StopAtDurationEnd, deserialized.StopAtDurationEnd);
  }

  [Fact]
  public void RepetitionPatternConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new RepetitionPatternConfig
    {
      Interval = TimeSpan.FromMinutes(5),
      Duration = TimeSpan.FromMinutes(30),
      StopAtDurationEnd = false
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<RepetitionPatternConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Interval, deserialized.Interval);
    Assert.Equal(original.Duration, deserialized.Duration);
    Assert.Equal(original.StopAtDurationEnd, deserialized.StopAtDurationEnd);
  }

  #endregion

  #region TriggerConfig Tests

  [Fact]
  public void TriggerConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new TriggerConfig();

    // Assert
    Assert.Equal(string.Empty, config.Type);
    Assert.True(config.Enabled);
    Assert.Null(config.StartBoundary);
    Assert.Null(config.EndBoundary);
    Assert.Null(config.ExecutionTimeLimit);
    Assert.Null(config.Id);
    Assert.Null(config.Repetition);
    Assert.Null(config.Delay);
  }

  [Fact]
  public void TriggerConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new TriggerConfig
    {
      Type = "time",
      Enabled = false,
      StartBoundary = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
      EndBoundary = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
      ExecutionTimeLimit = TimeSpan.FromMinutes(30),
      Id = "trigger-1",
      Repetition = new RepetitionPatternConfig
      {
        Interval = TimeSpan.FromMinutes(10),
        Duration = TimeSpan.FromHours(2),
        StopAtDurationEnd = true
      },
      Delay = TimeSpan.FromMinutes(5)
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<TriggerConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Type, deserialized.Type);
    Assert.Equal(original.Enabled, deserialized.Enabled);
    Assert.Equal(original.StartBoundary, deserialized.StartBoundary);
    Assert.Equal(original.EndBoundary, deserialized.EndBoundary);
    Assert.Equal(original.ExecutionTimeLimit, deserialized.ExecutionTimeLimit);
    Assert.Equal(original.Id, deserialized.Id);
    Assert.NotNull(deserialized.Repetition);
    Assert.Equal(original.Repetition.Interval, deserialized.Repetition.Interval);
    Assert.Equal(original.Repetition.Duration, deserialized.Repetition.Duration);
    Assert.Equal(original.Repetition.StopAtDurationEnd, deserialized.Repetition.StopAtDurationEnd);
    Assert.Equal(original.Delay, deserialized.Delay);
  }

  [Fact]
  public void TriggerConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new TriggerConfig
    {
      Type = "logon",
      Enabled = true,
      StartBoundary = new DateTime(2025, 2, 1, 9, 0, 0, DateTimeKind.Utc),
      ExecutionTimeLimit = TimeSpan.FromMinutes(15),
      Id = "trigger-2",
      Repetition = new RepetitionPatternConfig
      {
        Interval = TimeSpan.FromMinutes(2),
        Duration = TimeSpan.FromMinutes(10),
        StopAtDurationEnd = false
      }
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<TriggerConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Type, deserialized.Type);
    Assert.Equal(original.Enabled, deserialized.Enabled);
    Assert.Equal(original.StartBoundary, deserialized.StartBoundary);
    Assert.Equal(original.ExecutionTimeLimit, deserialized.ExecutionTimeLimit);
    Assert.Equal(original.Id, deserialized.Id);
    Assert.NotNull(deserialized.Repetition);
    Assert.Equal(original.Repetition.Interval, deserialized.Repetition.Interval);
    Assert.Equal(original.Repetition.Duration, deserialized.Repetition.Duration);
    Assert.Equal(original.Repetition.StopAtDurationEnd, deserialized.Repetition.StopAtDurationEnd);
  }

  #endregion

  #region ScheduledTaskConfig Tests

  [Fact]
  public void ScheduledTaskConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new ScheduledTaskConfig();

    // Assert
    Assert.Equal(string.Empty, config.Name);
    Assert.Equal(string.Empty, config.Path);
    Assert.Null(config.Description);
    Assert.Null(config.Author);
    Assert.NotNull(config.Triggers);
    Assert.Empty(config.Triggers);
    Assert.NotNull(config.Actions);
    Assert.Empty(config.Actions);
  }

  [Fact]
  public void ScheduledTaskConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new ScheduledTaskConfig
    {
      Name = "Daily Cleanup",
      Path = "\\WinHome\\Cleanup",
      Description = "Cleanup temp files",
      Author = "WinHome",
      Triggers = new List<TriggerConfig>
            {
                new TriggerConfig
                {
                    Type = "daily",
                    Enabled = true,
                    StartBoundary = new DateTime(2025, 3, 1, 2, 0, 0, DateTimeKind.Utc)
                }
            },
      Actions = new List<ActionConfig>
            {
                new ActionConfig
                {
                    Type = "exec",
                    Path = "C:\\Tools\\cleanup.ps1",
                    Arguments = "-Force"
                }
            }
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<ScheduledTaskConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.Path, deserialized.Path);
    Assert.Equal(original.Description, deserialized.Description);
    Assert.Equal(original.Author, deserialized.Author);
    Assert.NotNull(deserialized.Triggers);
    Assert.Single(deserialized.Triggers);
    Assert.Equal(original.Triggers[0].Type, deserialized.Triggers[0].Type);
    Assert.Equal(original.Triggers[0].StartBoundary, deserialized.Triggers[0].StartBoundary);
    Assert.NotNull(deserialized.Actions);
    Assert.Single(deserialized.Actions);
    Assert.Equal(original.Actions[0].Type, deserialized.Actions[0].Type);
    Assert.Equal(original.Actions[0].Path, deserialized.Actions[0].Path);
    Assert.Equal(original.Actions[0].Arguments, deserialized.Actions[0].Arguments);
  }

  [Fact]
  public void ScheduledTaskConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new ScheduledTaskConfig
    {
      Name = "Weekly Report",
      Path = "\\WinHome\\Report",
      Description = "Generate weekly report",
      Author = "WinHome",
      Triggers = new List<TriggerConfig>
            {
                new TriggerConfig
                {
                    Type = "weekly",
                    Enabled = false,
                    StartBoundary = new DateTime(2025, 4, 7, 6, 0, 0, DateTimeKind.Utc),
                    Repetition = new RepetitionPatternConfig
                    {
                        Interval = TimeSpan.FromMinutes(15),
                        Duration = TimeSpan.FromHours(1),
                        StopAtDurationEnd = true
                    }
                }
            },
      Actions = new List<ActionConfig>
            {
                new ActionConfig
                {
                    Type = "exec",
                    Path = "/usr/local/bin/report",
                    WorkingDirectory = "/var/reports"
                }
            }
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<ScheduledTaskConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.Path, deserialized.Path);
    Assert.Equal(original.Description, deserialized.Description);
    Assert.Equal(original.Author, deserialized.Author);
    Assert.NotNull(deserialized.Triggers);
    Assert.Single(deserialized.Triggers);
    Assert.Equal(original.Triggers[0].Type, deserialized.Triggers[0].Type);
    Assert.Equal(original.Triggers[0].StartBoundary, deserialized.Triggers[0].StartBoundary);
    Assert.NotNull(deserialized.Triggers[0].Repetition);
    Assert.Equal(original.Triggers[0].Repetition!.Interval, deserialized.Triggers[0].Repetition!.Interval);
    Assert.Equal(original.Triggers[0].Repetition!.Duration, deserialized.Triggers[0].Repetition!.Duration);
    Assert.Equal(original.Triggers[0].Repetition!.StopAtDurationEnd, deserialized.Triggers[0].Repetition!.StopAtDurationEnd);
    Assert.NotNull(deserialized.Actions);
    Assert.Single(deserialized.Actions);
    Assert.Equal(original.Actions[0].Type, deserialized.Actions[0].Type);
    Assert.Equal(original.Actions[0].Path, deserialized.Actions[0].Path);
    Assert.Equal(original.Actions[0].WorkingDirectory, deserialized.Actions[0].WorkingDirectory);
  }

  #endregion

  #region RegistryTweak Tests

  [Fact]
  public void RegistryTweak_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new RegistryTweak();

    // Assert
    Assert.Equal(string.Empty, config.Path);
    Assert.Equal(string.Empty, config.Name);
    Assert.NotNull(config.Value);
    Assert.Equal("string", config.Type);
  }

  [Fact]
  public void RegistryTweak_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new RegistryTweak
    {
      Path = "HKCU\\Software\\WinHome",
      Name = "TestValue",
      Value = 1,
      Type = "dword"
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<RegistryTweak>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Path, deserialized.Path);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.Type, deserialized.Type);

    var valueElement = Assert.IsType<JsonElement>(deserialized.Value);
    Assert.Equal(JsonValueKind.Number, valueElement.ValueKind);
    Assert.Equal(original.Value, valueElement.GetInt32());
  }

  [Fact]
  public void RegistryTweak_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new RegistryTweak
    {
      Path = "HKLM\\Software\\WinHome",
      Name = "TestString",
      Value = "Enabled",
      Type = "string"
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<RegistryTweak>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Path, deserialized.Path);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.Type, deserialized.Type);
    Assert.Equal(original.Value, deserialized.Value);
  }

  #endregion

  #region WslDistroConfig Tests

  [Fact]
  public void WslDistroConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new WslDistroConfig();

    // Assert
    Assert.Equal(string.Empty, config.Name);
    Assert.Null(config.SetupScript);
  }

  [Fact]
  public void WslDistroConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new WslDistroConfig
    {
      Name = "Ubuntu-22.04",
      SetupScript = "~/setup.sh"
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<WslDistroConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.SetupScript, deserialized.SetupScript);
  }

  [Fact]
  public void WslDistroConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new WslDistroConfig
    {
      Name = "Debian",
      SetupScript = "/opt/setup.sh"
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<WslDistroConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.SetupScript, deserialized.SetupScript);
  }

  #endregion

  #region WindowsServiceConfig Tests

  [Fact]
  public void WindowsServiceConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new WindowsServiceConfig();

    // Assert
    Assert.Equal(string.Empty, config.Name);
    Assert.Equal("running", config.State);
    Assert.Null(config.StartupType);
  }

  [Fact]
  public void WindowsServiceConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new WindowsServiceConfig
    {
      Name = "Spooler",
      State = "stopped",
      StartupType = "manual"
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<WindowsServiceConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.State, deserialized.State);
    Assert.Equal(original.StartupType, deserialized.StartupType);
  }

  [Fact]
  public void WindowsServiceConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new WindowsServiceConfig
    {
      Name = "W32Time",
      State = "running",
      StartupType = "automatic"
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<WindowsServiceConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Name, deserialized.Name);
    Assert.Equal(original.State, deserialized.State);
    Assert.Equal(original.StartupType, deserialized.StartupType);
  }

  #endregion

  #region DotfileConfig Tests

  [Fact]
  public void DotfileConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new DotfileConfig();

    // Assert
    Assert.Equal(string.Empty, config.Src);
    Assert.Equal(string.Empty, config.Target);
  }

  [Fact]
  public void DotfileConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new DotfileConfig
    {
      Src = "./dotfiles/.gitconfig",
      Target = "~/.gitconfig"
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<DotfileConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Src, deserialized.Src);
    Assert.Equal(original.Target, deserialized.Target);
  }

  [Fact]
  public void DotfileConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new DotfileConfig
    {
      Src = "./dotfiles/.vimrc",
      Target = "~/.vimrc"
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<DotfileConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.Src, deserialized.Src);
    Assert.Equal(original.Target, deserialized.Target);
  }

  #endregion

  #region ProfileConfig Tests

  [Fact]
  public void ProfileConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new ProfileConfig();

    // Assert
    Assert.Null(config.Git);
    Assert.NotNull(config.EnvVars);
    Assert.Empty(config.EnvVars);
  }

  [Fact]
  public void ProfileConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new ProfileConfig
    {
      Git = new GitConfig
      {
        UserName = "Profile User",
        UserEmail = "profile@example.com",
        CommitGpgSign = true,
        Settings = new Dictionary<string, string>
                {
                    { "core.editor", "code --wait" }
                }
      },
      EnvVars = new List<EnvVarConfig>
            {
                new EnvVarConfig { Variable = "Path", Value = "%USERPROFILE%\\work\\bin", Action = "append" }
            }
    };

    // Act
    var jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<ProfileConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.NotNull(deserialized.Git);
    Assert.Equal(original.Git.UserName, deserialized.Git.UserName);
    Assert.Equal(original.Git.UserEmail, deserialized.Git.UserEmail);
    Assert.Equal(original.Git.CommitGpgSign, deserialized.Git.CommitGpgSign);
    Assert.Equal(original.Git.Settings["core.editor"], deserialized.Git.Settings["core.editor"]);
    Assert.Single(deserialized.EnvVars);
    Assert.Equal(original.EnvVars[0].Variable, deserialized.EnvVars[0].Variable);
    Assert.Equal(original.EnvVars[0].Value, deserialized.EnvVars[0].Value);
    Assert.Equal(original.EnvVars[0].Action, deserialized.EnvVars[0].Action);
  }

  [Fact]
  public void ProfileConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new ProfileConfig
    {
      Git = new GitConfig
      {
        UserName = "Yaml User",
        UserEmail = "yaml@example.com",
        SigningKey = "ABC123",
        CommitGpgSign = false,
        Settings = new Dictionary<string, string>
                {
                    { "core.autocrlf", "true" }
                }
      },
      EnvVars = new List<EnvVarConfig>
            {
                new EnvVarConfig { Variable = "EDITOR", Value = "code", Action = "set" }
            }
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    var yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<ProfileConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.NotNull(deserialized.Git);
    Assert.Equal(original.Git.UserName, deserialized.Git.UserName);
    Assert.Equal(original.Git.UserEmail, deserialized.Git.UserEmail);
    Assert.Equal(original.Git.SigningKey, deserialized.Git.SigningKey);
    Assert.Equal(original.Git.CommitGpgSign, deserialized.Git.CommitGpgSign);
    Assert.Equal(original.Git.Settings["core.autocrlf"], deserialized.Git.Settings["core.autocrlf"]);
    Assert.Single(deserialized.EnvVars);
    Assert.Equal(original.EnvVars[0].Variable, deserialized.EnvVars[0].Variable);
    Assert.Equal(original.EnvVars[0].Value, deserialized.EnvVars[0].Value);
    Assert.Equal(original.EnvVars[0].Action, deserialized.EnvVars[0].Action);
  }

  #endregion

  #region GitConfig Tests

  [Fact]
  public void GitConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new GitConfig();

    // Assert
    Assert.Null(config.UserName);
    Assert.Null(config.UserEmail);
    Assert.Null(config.SigningKey);
    Assert.Null(config.CommitGpgSign);
    Assert.NotNull(config.Settings);
    Assert.Empty(config.Settings);
  }

  [Fact]
  public void GitConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new GitConfig
    {
      UserName = "Dev Explorer",
      UserEmail = "dev@example.com",
      SigningKey = "ABC123XYZ",
      CommitGpgSign = true,
      Settings = new Dictionary<string, string>
            {
                { "core.editor", "code --wait" },
                { "init.defaultBranch", "main" }
            }
    };

    // Act
    string jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<GitConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.UserName, deserialized.UserName);
    Assert.Equal(original.UserEmail, deserialized.UserEmail);
    Assert.Equal(original.SigningKey, deserialized.SigningKey);
    Assert.Equal(original.CommitGpgSign, deserialized.CommitGpgSign);

    // Assert dictionary values
    Assert.NotNull(deserialized.Settings);
    Assert.Equal(original.Settings["core.editor"], deserialized.Settings["core.editor"]);
    Assert.Equal(original.Settings["init.defaultBranch"], deserialized.Settings["init.defaultBranch"]);

  }

  [Fact]
  public void GitConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new GitConfig
    {
      UserName = "Dev Explorer",
      UserEmail = "dev@example.com",
      SigningKey = "GPG9876",
      CommitGpgSign = false,
      Settings = new Dictionary<string, string>
            {
                { "core.autocrlf", "true" }
            }
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    string yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<GitConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.UserName, deserialized.UserName);
    Assert.Equal(original.UserEmail, deserialized.UserEmail);
    Assert.Equal(original.SigningKey, deserialized.SigningKey);
    Assert.Equal(original.CommitGpgSign, deserialized.CommitGpgSign);

    Assert.NotNull(deserialized.Settings);
    Assert.Equal(original.Settings["core.autocrlf"], deserialized.Settings["core.autocrlf"]);
  }
  #endregion

  #region WslConfig Tests

  [Fact]
  public void WslConfig_ShouldInitializeWithDefaults()
  {
    // Arrange & Act
    var config = new WslConfig();

    // Assert
    Assert.Equal(2, config.DefaultVersion);
    Assert.Null(config.DefaultDistro);
    Assert.False(config.Update);

    // List should be initialized but empty
    Assert.NotNull(config.Distros);
    Assert.Empty(config.Distros);
  }

  [Fact]
  public void WslConfig_ShouldRoundTrip_JsonSerialization()
  {
    // Arrange
    var original = new WslConfig
    {
      DefaultVersion = 2,
      DefaultDistro = "Ubuntu",
      Update = true,
      Distros = new List<WslDistroConfig>
            {
                new WslDistroConfig { Name = "Ubuntu-22.04" },
                new WslDistroConfig { Name = "Debian" }
            }
    };

    // Act
    string jsonString = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<WslConfig>(jsonString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.DefaultVersion, deserialized.DefaultVersion);
    Assert.Equal(original.DefaultDistro, deserialized.DefaultDistro);
    Assert.Equal(original.Update, deserialized.Update);

    // Verify nested list data
    Assert.NotNull(deserialized.Distros);
    Assert.Equal(original.Distros.Count, deserialized.Distros.Count);
    Assert.Equal(original.Distros[0].Name, deserialized.Distros[0].Name);
    Assert.Equal(original.Distros[1].Name, deserialized.Distros[1].Name);
  }

  [Fact]
  public void WslConfig_ShouldRoundTrip_YamlSerialization()
  {
    // Arrange
    var original = new WslConfig
    {
      DefaultVersion = 1,
      DefaultDistro = "Alpine",
      Update = false,
      Distros = new List<WslDistroConfig>
            {
                new WslDistroConfig { Name = "Alpine" }
            }
    };

    var serializer = new SerializerBuilder().Build();
    var deserializer = new DeserializerBuilder().Build();

    // Act
    string yamlString = serializer.Serialize(original);
    var deserialized = deserializer.Deserialize<WslConfig>(yamlString);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal(original.DefaultVersion, deserialized.DefaultVersion);
    Assert.Equal(original.DefaultDistro, deserialized.DefaultDistro);
    Assert.Equal(original.Update, deserialized.Update);

    Assert.NotNull(deserialized.Distros);
    Assert.Single(deserialized.Distros);
    Assert.Equal(original.Distros[0].Name, deserialized.Distros[0].Name);
  }

  #endregion

}
