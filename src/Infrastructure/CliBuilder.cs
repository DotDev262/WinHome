using System.CommandLine;
using System.CommandLine.Parsing;
using WinHome.Interfaces;
using WinHome.Models;
using YamlDotNet.Serialization;

namespace WinHome.Infrastructure;

public static class CliBuilder
{
    public static RootCommand BuildRootCommand(
        Func<FileInfo, bool, string?, bool, bool, bool, bool, Task<int>> runAction,
        Func<FileInfo?, Task<int>> generateAction)
    {
        var configOption = new Option<FileInfo>("--config")
        {
            Description = "Path to the YAML configuration file",
            DefaultValueFactory = _ =>
            {
                var configPath = Environment.GetEnvironmentVariable("WINHOME_CONFIG_PATH");
                return new FileInfo(string.IsNullOrEmpty(configPath) ? "config.yaml" : configPath);
            }
        };

        var updateOption = new Option<bool>("--update")
        {
            Description = "Check for updates and upgrade if available",
            DefaultValueFactory = _ => false
        };
        updateOption.Aliases.Add("-u");

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

        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output results as JSON",
            DefaultValueFactory = _ => false
        };

        var rootCommand = new RootCommand("WinHome: Windows Setup Tool");
        rootCommand.Options.Add(configOption);
        rootCommand.Options.Add(updateOption);
        rootCommand.Options.Add(dryRunOption);
        rootCommand.Options.Add(profileOption);
        rootCommand.Options.Add(debugOption);
        rootCommand.Options.Add(diffOption);
        rootCommand.Options.Add(jsonOption);

        rootCommand.SetAction(async (ParseResult result) =>
        {
            FileInfo file = result.GetValue(configOption)!;
            bool update = result.GetValue(updateOption);
            bool dryRun = result.GetValue(dryRunOption);
            string? profile = result.GetValue(profileOption);
            bool debug = result.GetValue(debugOption);
            bool diff = result.GetValue(diffOption);
            bool json = result.GetValue(jsonOption);

            return await runAction(file, dryRun, profile, debug, diff, json, update);
        });

        // Generate Command
        var generateCommand = new Command("generate", "Generate a configuration file from the current system state");
        var outputOption = new Option<FileInfo?>("--output", "Output file path (default: stdout)");
        outputOption.Aliases.Add("-o");
        generateCommand.Options.Add(outputOption);

        generateCommand.SetAction(async (ParseResult result) =>
        {
            FileInfo? output = result.GetValue(outputOption);
            return await generateAction(output);
        });

        rootCommand.Add(generateCommand);

        return rootCommand;
    }
}
