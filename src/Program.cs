using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Parsing;
using WinHome;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Bootstrappers;
using WinHome.Services.Logging;
using WinHome.Services.Managers;
using WinHome.Services.System;
using YamlDotNet.Serialization;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output results as JSON",
            DefaultValueFactory = _ => false
        };

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var isJson = context.Configuration.GetValue<bool>("json");

                if (isJson)
                {
                    services.AddSingleton<ILogger, JsonLogger>();
                }
                else
                {
                    services.AddSingleton<ILogger, ConsoleLogger>();
                }

                services.AddSingleton<DotfileService>(sp => new DotfileService(sp.GetRequiredService<ILogger>()));
                services.AddSingleton<RegistryService>(sp => new RegistryService(sp.GetRequiredService<IRegistryWrapper>()));
                services.AddSingleton<SystemSettingsService>(sp => new SystemSettingsService(sp.GetRequiredService<IProcessRunner>()));
                services.AddSingleton<WslService>(sp => new WslService(sp.GetRequiredService<IProcessRunner>(), sp.GetRequiredService<ILogger>()));
                services.AddSingleton<GitService>(sp => new GitService(sp.GetRequiredService<IProcessRunner>(), sp.GetRequiredService<ILogger>()));
                services.AddSingleton<EnvironmentService>(sp => new EnvironmentService(sp.GetRequiredService<ILogger>()));
                services.AddSingleton<WindowsServiceManager>();
                services.AddSingleton<ScheduledTaskService>();

                // Bootstrappers
                services.AddSingleton<ChocolateyBootstrapper>();
                services.AddSingleton<ScoopBootstrapper>();
                services.AddSingleton<WingetBootstrapper>();

                // Package Managers
                services.AddSingleton<WingetService>(sp => new WingetService(sp.GetRequiredService<IProcessRunner>(), sp.GetRequiredService<WingetBootstrapper>(), sp.GetRequiredService<ILogger>()));
                services.AddSingleton<ChocolateyService>(sp => new ChocolateyService(sp.GetRequiredService<IProcessRunner>(), sp.GetRequiredService<ChocolateyBootstrapper>(), sp.GetRequiredService<ILogger>()));
                services.AddSingleton<ScoopService>(sp => new ScoopService(sp.GetRequiredService<IProcessRunner>(), sp.GetRequiredService<ScoopBootstrapper>(), sp.GetRequiredService<ILogger>()));

                services.AddSingleton<IDotfileService, DotfileService>();
                services.AddSingleton<IRegistryService, RegistryService>();
                services.AddSingleton<IRegistryWrapper, RegistryWrapper>();
                services.AddSingleton<ISystemSettingsService, SystemSettingsService>();
                services.AddSingleton<IWslService, WslService>();
                services.AddSingleton<IGitService, GitService>();
                services.AddSingleton<IEnvironmentService, EnvironmentService>();
                services.AddSingleton<IWindowsServiceManager, WindowsServiceManager>();
                services.AddSingleton<IServiceControllerWrapper, ServiceControllerWrapper>();
                services.AddSingleton<IProcessRunner, DefaultProcessRunner>();
                services.AddSingleton<IScheduledTaskService, ScheduledTaskService>();
                services.AddSingleton<Dictionary<string, IPackageManager>>(sp => new()
                {
                    { "winget", sp.GetRequiredService<WingetService>() },
                    { "choco", sp.GetRequiredService<ChocolateyService>() },
                    { "scoop", sp.GetRequiredService<ScoopService>() }
                });
                services.AddSingleton<Engine>(sp => new Engine(
                    sp.GetRequiredService<Dictionary<string, IPackageManager>>(),
                    sp.GetRequiredService<IDotfileService>(),
                    sp.GetRequiredService<IRegistryService>(),
                    sp.GetRequiredService<ISystemSettingsService>(),
                    sp.GetRequiredService<IWslService>(),
                    sp.GetRequiredService<IGitService>(),
                    sp.GetRequiredService<IEnvironmentService>(),
                    sp.GetRequiredService<IWindowsServiceManager>(),
                    sp.GetRequiredService<IScheduledTaskService>(),
                    sp.GetRequiredService<ILogger>()
                ));
            })
            .Build();

        var configOption = new Option<FileInfo>("--config")
        {
            Description = "Path to the YAML configuration file",
            DefaultValueFactory = _ =>
            {
                var configPath = Environment.GetEnvironmentVariable("WINHOME_CONFIG_PATH");
                return new FileInfo(string.IsNullOrEmpty(configPath) ? "config.yaml" : configPath);
            }
        };

        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Preview changes without applying them",
            DefaultValueFactory = _ => false
        };
        dryRunOption.Aliases.Add("-d");

        var profileOption = new Option<string?>("--profile")
        {
            Description = "Activate a specific profile (e.g. work, personal)",
            DefaultValueFactory = _ => null
        };
        profileOption.Aliases.Add("-p");


        var debugOption = new Option<bool>("--debug")
        {
            Description = "Enable verbose logging and configuration validation",
            DefaultValueFactory = _ => false
        };

        var diffOption = new Option<bool>("--diff")
        {
            Description = "Show a diff of the changes that will be made",
            DefaultValueFactory = _ => false
        };

        var rootCommand = new RootCommand("WinHome: Windows Setup Tool");
        rootCommand.Options.Add(configOption);
        rootCommand.Options.Add(dryRunOption);
        rootCommand.Options.Add(profileOption);
        rootCommand.Options.Add(debugOption);
        rootCommand.Options.Add(diffOption);
        rootCommand.Options.Add(jsonOption);

        rootCommand.SetAction(async (ParseResult result) =>
        {
            FileInfo file = result.GetValue(configOption)!;
            bool dryRun = result.GetValue(dryRunOption);
            string? profile = result.GetValue(profileOption);
            bool debug = result.GetValue(debugOption);
            bool diff = result.GetValue(diffOption);
            bool json = result.GetValue(jsonOption);

            var engine = host.Services.GetRequiredService<Engine>();
            var logger = host.Services.GetRequiredService<ILogger>();
            var exitCode = await RunAppAsync(engine, logger, file, dryRun, profile, debug, diff, json);
            
            if (logger is JsonLogger jsonLogger)
            {
                Console.WriteLine(jsonLogger.ToJson());
            }

            return exitCode;
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    static async Task<int> RunAppAsync(Engine engine, ILogger logger, FileInfo file, bool dryRun, string? profile, bool debug, bool diff, bool json)
    {
        if (!file.Exists)
        {
            logger.LogError($"[Error] Config file not found at: {file.FullName}");
            return 1;
        }

        try
        {
            if (debug) logger.LogInfo($"[Debug] Reading config from: {file.FullName}");

            var yamlText = File.ReadAllText(file.FullName);

            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<Configuration>(yamlText);


            if (debug)
            {
                logger.LogInfo("\n=== DEBUG: Configuration Dump ===");
                logger.LogInfo($"- Version: {config.Version}");
                logger.LogInfo($"- Apps: {config.Apps.Count} found");
                logger.LogInfo($"- Dotfiles: {config.Dotfiles.Count} found");
                logger.LogInfo($"- Env Vars: {config.EnvVars.Count} found");

                if (config.Wsl != null)
                    logger.LogInfo($"- WSL: Enabled (Update={config.Wsl.Update}, Distros={config.Wsl.Distros.Count})");
                else
                    logger.LogInfo("- WSL: Not configured (null)");

                if (config.Git != null)
                    logger.LogInfo($"- Git: Enabled (User={config.Git.UserName})");

                if (config.SystemSettings.Any())
                {
                    logger.LogInfo("- System Settings:");
                    foreach (var kvp in config.SystemSettings)
                        logger.LogInfo($"  * {kvp.Key}: {kvp.Value}");
                }

                logger.LogInfo("=================================\n");
            }


            if (dryRun)
            {
                logger.LogWarning("--- DRY RUN MODE: No changes will be made ---");
            }

            await engine.RunAsync(config, dryRun, profile, debug, diff);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError($"[Critical Error] {ex.Message}");


            if (debug)
            {
                if (ex.StackTrace != null)
                {
                    logger.LogError(ex.StackTrace);
                }
                if (ex.InnerException != null)
                {
                    logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            else
            {
                logger.LogError("Tip: Run with --debug to see full error details.");
            }
            return 1;
        }
    }
}