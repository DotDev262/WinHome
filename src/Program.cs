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
            }
        );

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
