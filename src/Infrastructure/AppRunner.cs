using WinHome.Interfaces;
using WinHome.Models;
using YamlDotNet.Serialization;

namespace WinHome.Infrastructure;

public class AppRunner
{
    private readonly Engine _engine;
    private readonly ILogger _logger;
    private readonly IConfigValidator _validator;

    public AppRunner(Engine engine, ILogger logger, IConfigValidator validator)
    {
        _engine = engine;
        _logger = logger;
        _validator = validator;
    }

    public async Task<int> RunAsync(FileInfo file, bool dryRun, string? profile, bool debug, bool diff, bool json)
    {
        if (!file.Exists)
        {
            _logger.LogError($"[Error] Config file not found at: {file.FullName}");
            return 1;
        }

        try
        {
            if (debug) _logger.LogInfo($"[Debug] Reading config from: {file.FullName}");

            var yamlText = await File.ReadAllTextAsync(file.FullName);

            // Schema Validation
            var (isValid, errors) = _validator.Validate(yamlText);
            if (!isValid)
            {
                _logger.LogError("[Error] Configuration validation failed:");
                foreach (var error in errors)
                {
                    _logger.LogError($"  - {error}");
                }
                return 1;
            }

            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<Configuration>(yamlText);

            if (debug)
            {
                LogDebugInfo(config);
            }

            if (dryRun)
            {
                _logger.LogWarning("--- DRY RUN MODE: No changes will be made ---");
            }

            await _engine.RunAsync(config, dryRun, profile, debug, diff);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Critical Error] {ex.Message}");

            if (debug)
            {
                if (ex.StackTrace != null)
                {
                    _logger.LogError(ex.StackTrace);
                }
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            else
            {
                _logger.LogError("Tip: Run with --debug to see full error details.");
            }
            return 1;
        }
    }

    private void LogDebugInfo(Configuration config)
    {
        _logger.LogInfo("\n=== DEBUG: Configuration Dump ===");
        _logger.LogInfo($"- Version: {config.Version}");
        _logger.LogInfo($"- Apps: {config.Apps.Count} found");
        _logger.LogInfo($"- Dotfiles: {config.Dotfiles.Count} found");
        _logger.LogInfo($"- Env Vars: {config.EnvVars.Count} found");

        if (config.Wsl != null)
            _logger.LogInfo($"- WSL: Enabled (Update={config.Wsl.Update}, Distros={config.Wsl.Distros.Count})");
        else
            _logger.LogInfo("- WSL: Not configured (null)");

        if (config.Git != null)
            _logger.LogInfo($"- Git: Enabled (User={config.Git.UserName})");

        if (config.SystemSettings.Any())
        {
            _logger.LogInfo("- System Settings:");
            foreach (var kvp in config.SystemSettings)
                _logger.LogInfo($"  * {kvp.Key}: {kvp.Value}");
        }

        _logger.LogInfo("=================================\n");
    }
}
