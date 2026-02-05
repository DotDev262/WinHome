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
        Func<FileInfo?, Task<int>> generateAction,
        Func<string, string?, Task<int>> stateAction)
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
        var outputOption = new Option<FileInfo?>("--output")
        {
            Description = "Output file path (default: stdout)"
        };
        outputOption.Aliases.Add("-o");
        generateCommand.Options.Add(outputOption);

        generateCommand.SetAction(async (ParseResult result) =>
        {
            FileInfo? output = result.GetValue(outputOption);
            return await generateAction(output);
        });

        rootCommand.Add(generateCommand);

        // State Command
        var stateCommand = new Command("state", "Manage the system state managed by WinHome");
        
        var listSubCommand = new Command("list", "List all items currently managed by WinHome");
        listSubCommand.SetAction(async (ParseResult result) =>
        {
            return await stateAction("list", null);
        });

        var backupSubCommand = new Command("backup", "Backup the current state file");
        var backupPathArgument = new Argument<string>("path") { Description = "Path to save the backup" };
        backupSubCommand.Arguments.Add(backupPathArgument);
        backupSubCommand.SetAction(async (ParseResult result) =>
        {
            var path = result.GetValue(backupPathArgument);
            return await stateAction("backup", path);
        });

        var restoreSubCommand = new Command("restore", "Restore the state file from a backup");
        var restorePathArgument = new Argument<string>("path") { Description = "Path to the backup file to restore" };
        restoreSubCommand.Arguments.Add(restorePathArgument);
        restoreSubCommand.SetAction(async (ParseResult result) =>
        {
            var path = result.GetValue(restorePathArgument);
            return await stateAction("restore", path);
        });

        stateCommand.Subcommands.Add(listSubCommand);
        stateCommand.Subcommands.Add(backupSubCommand);
        stateCommand.Subcommands.Add(restoreSubCommand);

        rootCommand.Add(stateCommand);

        return rootCommand;
    }
}
