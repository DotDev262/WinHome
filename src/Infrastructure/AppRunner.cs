using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinHome.Infrastructure;

/// <summary>Orchestrates configuration loading, validation, secret resolution, and engine execution.</summary>
public class AppRunner
{
  private readonly IEngine _engine;
  private readonly IConfigValidator _validator;
  private readonly ISecretResolver _secretResolver;
  private readonly ILogger _logger;

  /// <summary>Initializes a new instance of <see cref="AppRunner"/>.</summary>
  public AppRunner(IEngine engine, IConfigValidator validator, ISecretResolver secretResolver, ILogger logger)
  {
    _engine = engine;
    _validator = validator;
    _secretResolver = secretResolver;
    _logger = logger;
  }

  /// <summary>Loads, validates, resolves secrets in, and applies the given configuration file.</summary>
  /// <param name="configFile">Path to the YAML configuration file.</param>
  /// <param name="dryRun">If <c>true</c>, previews changes without applying.</param>
  /// <param name="profile">Optional named profile to activate.</param>
  /// <param name="debug">If <c>true</c>, shows detailed error information.</param>
  /// <param name="diff">If <c>true</c>, shows a diff of changes.</param>
  /// <param name="json">If <c>true</c>, outputs results as JSON.</param>
  /// <param name="force">If <c>true</c>, reapplies steps even if previously succeeded.</param>
  /// <param name="continueOnError">If <c>true</c>, continues applying remaining steps on failure.</param>
  /// <returns>Exit code (0 for success).</returns>
  public async Task<int> RunAsync(FileInfo configFile, bool dryRun, string? profile, bool debug, bool diff, bool json, bool force = false, bool continueOnError = false)
  {
    try
    {
      if (!configFile.Exists)
      {
        _logger.LogError($"[Error] Configuration file not found: {configFile.FullName}");
        return 1;
      }

      var yamlContent = await File.ReadAllTextAsync(configFile.FullName);

      var validation = _validator.Validate(yamlContent);
      if (!validation.IsValid)
      {
        _logger.LogError("[Error] Configuration validation failed:");
        foreach (var err in validation.Errors) _logger.LogError($"  - {err}");
        return 1;
      }

      var deserializer = new DeserializerBuilder()
          .WithNamingConvention(CamelCaseNamingConvention.Instance)
          .IgnoreUnmatchedProperties()
          .Build();

      var config = deserializer.Deserialize<Configuration>(yamlContent);

      // Resolve Secrets
      _secretResolver.ResolveObject(config);

      await _engine.RunAsync(config, dryRun, profile, debug, diff, force, continueOnError);
      return 0;
    }
    catch (Exception ex)
    {
      _logger.LogError($"[Fatal] An unexpected error occurred: {ex.Message}");
      if (debug) _logger.LogError(ex.StackTrace ?? "");
      return 1;
    }
  }
}
