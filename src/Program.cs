using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using WinHome.Infrastructure;
using WinHome.Interfaces;
using WinHome.Services.Logging;

class Program
{
    static async Task<int> Main(string[] args)
    {
        using IHost host = AppHost.CreateHost(args);

        var rootCommand = CliBuilder.BuildRootCommand(async (file, dryRun, profile, debug, diff, json, update) =>
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
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
