using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinHome.Infrastructure;

public class AppRunner
{
    private readonly Engine _engine;
    private readonly IConfigValidator _validator;
    private readonly ISecretResolver _secretResolver;
    private readonly ILogger _logger;

    public AppRunner(Engine engine, IConfigValidator validator, ISecretResolver secretResolver, ILogger logger)
    {
        _engine = engine;
        _validator = validator;
        _secretResolver = secretResolver;
        _logger = logger;
    }

    public async Task<int> RunAsync(FileInfo configFile, bool dryRun, string? profile, bool debug, bool diff, bool json)
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

            await _engine.RunAsync(config, dryRun, profile, debug, diff);
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
