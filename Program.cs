using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Parsing;
using WinHome;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;
using WinHome.Services.System;
using YamlDotNet.Serialization;

class Program
{
    static async Task<int> Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<DotfileService>();
                services.AddSingleton<RegistryService>();
                services.AddSingleton<SystemSettingsService>();
                services.AddSingleton<WslService>();
                services.AddSingleton<GitService>();
                services.AddSingleton<EnvironmentService>();
                services.AddSingleton<WindowsServiceManager>();
                services.AddSingleton<ScheduledTaskService>();
                services.AddSingleton<WingetService>(sp => new WingetService(sp.GetRequiredService<IProcessRunner>()));
                services.AddSingleton<ChocolateyService>(sp => new ChocolateyService(sp.GetRequiredService<IProcessRunner>()));
                services.AddSingleton<ScoopService>(sp => new ScoopService(sp.GetRequiredService<IProcessRunner>()));
                services.AddSingleton<MiseService>(sp => new MiseService(sp.GetRequiredService<IProcessRunner>()));
                services.AddSingleton<IDotfileService, DotfileService>();
                services.AddSingleton<IRegistryService, RegistryService>();
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
                    { "scoop", sp.GetRequiredService<ScoopService>() },
                    { "mise", sp.GetRequiredService<MiseService>() }
                });
                services.AddSingleton<Engine>();
            })
            .Build();

        var configOption = new Option<FileInfo>("--config")
        {
            Description = "Path to the YAML configuration file",
            DefaultValueFactory = _ => new FileInfo("config.yaml")
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

        var rootCommand = new RootCommand("WinHome: Windows Setup Tool");
        rootCommand.Options.Add(configOption);
        rootCommand.Options.Add(dryRunOption);
        rootCommand.Options.Add(profileOption);
        rootCommand.Options.Add(debugOption);

        rootCommand.SetAction(async (ParseResult result) => 
        {
            FileInfo file = result.GetValue(configOption)!;
            bool dryRun = result.GetValue(dryRunOption);
            string? profile = result.GetValue(profileOption);
            bool debug = result.GetValue(debugOption);
            
            var engine = host.Services.GetRequiredService<Engine>();
            await RunAppAsync(engine, file, dryRun, profile, debug);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    static async Task RunAppAsync(Engine engine, FileInfo file, bool dryRun, string? profile, bool debug)
    {
        if (!file.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error] Config file not found at: {file.FullName}");
            Console.ResetColor();
            return;
        }

        try 
        {
            if (debug) Console.WriteLine($"[Debug] Reading config from: {file.FullName}");

            var yamlText = File.ReadAllText(file.FullName);
            
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<Configuration>(yamlText);

            
            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n=== DEBUG: Configuration Dump ===");
                Console.WriteLine($"- Version: {config.Version}");
                Console.WriteLine($"- Apps: {config.Apps.Count} found");
                Console.WriteLine($"- Dotfiles: {config.Dotfiles.Count} found");
                Console.WriteLine($"- Env Vars: {config.EnvVars.Count} found");
                
                if (config.Wsl != null)
                    Console.WriteLine($"- WSL: Enabled (Update={config.Wsl.Update}, Distros={config.Wsl.Distros.Count})");
                else
                    Console.WriteLine("- WSL: Not configured (null)");

                if (config.Git != null)
                    Console.WriteLine($"- Git: Enabled (User={config.Git.UserName})");
                
                if (config.SystemSettings.Any())
                {
                    Console.WriteLine("- System Settings:");
                    foreach(var kvp in config.SystemSettings)
                        Console.WriteLine($"  * {kvp.Key}: {kvp.Value}");
                }
                
                Console.WriteLine("=================================\n");
                Console.ResetColor();
            }

            
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("--- DRY RUN MODE: No changes will be made ---");
                Console.ResetColor();
            }

            await engine.RunAsync(config, dryRun, profile);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Critical Error] {ex.Message}");
            
            
            if (debug)
            {
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            else
            {
                Console.WriteLine("Tip: Run with --debug to see full error details.");
            }
            Console.ResetColor();
        }
    }
}