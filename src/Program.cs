using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using WinHome.Infrastructure;
using WinHome.Interfaces;
using WinHome.Services.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            using IHost host = AppHost.CreateHost(args);

            var rootCommand = CliBuilder.BuildRootCommand(
                // Run Action
                async (file, dryRun, profile, debug, diff, json, update) =>
                {
                    var logger = host.Services.GetRequiredService<ILogger>();

                    if (update)
                    {
                        var updater = host.Services.GetRequiredService<IUpdateService>();
                        // In a real app, get version from Assembly
                        var currentVersion = "1.0.0"; 
                        if (await updater.CheckForUpdatesAsync(currentVersion))
                        {
                            await updater.UpdateAsync();
                        }
                        return 0;
                    }

                    var runner = host.Services.GetRequiredService<AppRunner>();
                    
                    var exitCode = await runner.RunAsync(file, dryRun, profile, debug, diff, json);

                    if (logger is JsonLogger jsonLogger)
                    {
                        Console.WriteLine(jsonLogger.ToJson());
                    }

                    return exitCode;
                },
                // Generate Action
                async (outputFile) =>
                {
                    var generator = host.Services.GetRequiredService<IGeneratorService>();
                    var logger = host.Services.GetRequiredService<ILogger>();

                    try
                    {
                        var config = await generator.GenerateAsync();
                        
                        var serializer = new SerializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                            .Build();

                        var yaml = serializer.Serialize(config);

                        if (outputFile != null)
                        {
                            await File.WriteAllTextAsync(outputFile.FullName, yaml);
                            logger.LogSuccess($"[Generator] Configuration saved to {outputFile.FullName}");
                        }
                        else
                        {
                            Console.WriteLine(yaml);
                        }
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"[Generator] Failed to generate configuration: {ex.Message}");
                        return 1;
                    }
                },
                // State Action
                async (command, path) =>
                {
                    var stateService = host.Services.GetRequiredService<IStateService>();
                    var logger = host.Services.GetRequiredService<ILogger>();

                    switch (command)
                    {
                        case "list":
                            var items = stateService.ListItems();
                            if (!items.Any())
                            {
                                logger.LogInfo("[State] No items are currently managed by WinHome.");
                            }
                            else
                            {
                                logger.LogInfo("\n--- Managed Items ---");
                                foreach (var item in items)
                                {
                                    Console.WriteLine($"  - {item}");
                                }
                            }
                            break;
                        case "backup":
                            if (string.IsNullOrEmpty(path)) return 1;
                            stateService.BackupState(path);
                            break;
                        case "restore":
                            if (string.IsNullOrEmpty(path)) return 1;
                            stateService.RestoreState(path);
                            break;
                    }
                    return 0;
                }
            );

            return await rootCommand.Parse(args).InvokeAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Fatal Error] An unhandled exception occurred:");
            Console.WriteLine(ex.Message);
            
            if (args.Contains("--debug"))
            {
                Console.WriteLine(ex.StackTrace);
            }
            Console.ResetColor();
            return 1;
        }
    }
}
