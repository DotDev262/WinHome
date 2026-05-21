using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinHome.Infrastructure;

/// <summary>
/// Orchestrates the baseline ingestion workflow for the management engine, handling file I/O operations, 
/// structural schema validation, secret decryption overlays, and execution loop routing.
/// </summary>
public class AppRunner
{
    private readonly Engine _engine;
    private readonly IConfigValidator _validator;
    private readonly ISecretResolver _secretResolver;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppRunner"/> class with downstream processing engines and validation handlers.
    /// </summary>
    /// <param name="engine">The central execution state reconciliation engine processing structural domains.</param>
    /// <param name="validator">The textual verification validation service verifying incoming YAML compliance metrics.</param>
    /// <param name="secretResolver">The secure text mutation vault broker decrypting hidden property strings in-memory.</param>
    /// <param name="logger">The diagnostic log tracker capture utility routing system exceptions or metrics.</param>
    public AppRunner(Engine engine, IConfigValidator validator, ISecretResolver secretResolver, ILogger logger)
    {
        _engine = engine;
        _validator = validator;
        _secretResolver = secretResolver;
        _logger = logger;
    }

    /// <summary>
    /// Executes the primary operational environment validation and synchronization pipeline asynchronously.
    /// </summary>
    /// <param name="configFile">The target <see cref="FileInfo"/> representing the local disk path of the requested configuration manifest.</param>
    /// <param name="dryRun">A conditional flag which, when <c>true</c>, checks for environmental drifts without executing state mutations.</param>
    /// <param name="profile">An optional target tag string to filter execution passes down to a specific named sub-profile.</param>
    /// <param name="debug">A diagnostic control modifier forcing underlying routines to write expanded stack traces to logs upon exceptions.</param>
    /// <param name="diff">A toggle tracking whether the core synchronization loop isolates behavior exclusively to displaying delta changes.</param>
    /// <param name="json">A layout switch indicator identifying whether downstream execution states should explicitly pipe back structured text models.</param>
    /// <returns>An asynchronous task containing an exit operational code integer, where <c>0</c> indicates clean termination and <c>1</c> denotes execution failures.</returns>
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