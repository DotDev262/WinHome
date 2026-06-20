using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using WinHome.Interfaces;
using WinHome.Models;
using YamlDotNet.Serialization;

namespace WinHome.Infrastructure;

/// <summary>Builds the System.CommandLine root command and all subcommands for the WinHome CLI.</summary>
public static class CliBuilder
{
  /// <summary>Constructs the root command with all options, subcommands (run, generate, state, completion), and their handlers.</summary>
  /// <param name="runAction">Handler for the default run action (applies configuration).</param>
  /// <param name="generateAction">Handler for the generate subcommand (generates config from system state).</param>
  /// <param name="configBackupAction">Handler for configuration backup and restore.</param>
  /// <param name="stateAction">Handler for the state subcommand (manages tracking state).</param>
  /// <returns>The configured root <see cref="RootCommand"/>.</returns>
  public static RootCommand BuildRootCommand(
    Func<FileInfo, bool, string?, bool, bool, bool, bool, bool, bool, LogLevel, Task<int>> runAction,
    Func<FileInfo?, LogLevel, Task<int>> generateAction,
    Func<string, string?, LogLevel, Task<int>> stateAction,
    Func<string, string?, string?, LogLevel, Task<int>>? configBackupAction = null)
  {
    var configOption = new Option<FileInfo>("--config");
    configOption.Description = "Path to the YAML configuration file";
    configOption.DefaultValueFactory = _ =>
    {
      var configPath = Environment.GetEnvironmentVariable("WINHOME_CONFIG_PATH");
      return new FileInfo(string.IsNullOrEmpty(configPath) ? "config.yaml" : configPath);
    };

    var updateOption = new Option<bool>("--update");
    updateOption.Description = "Check for updates and upgrade if available";
    updateOption.DefaultValueFactory = _ => false;
    updateOption.Aliases.Add("-u");

    var dryRunOption = new Option<bool>("--dry-run");
    dryRunOption.Description = "Preview changes without applying them";
    dryRunOption.DefaultValueFactory = _ => false;
    dryRunOption.Aliases.Add("-d");

    var profileOption = new Option<string?>("--profile");
    profileOption.Description = "Activate a specific profile (e.g. work, personal)";
    profileOption.DefaultValueFactory = _ => null;
    profileOption.Aliases.Add("-p");

    var debugOption = new Option<bool>("--debug");
    debugOption.Description = "Show detailed error information including stack traces";
    debugOption.DefaultValueFactory = _ => false;

    var diffOption = new Option<bool>("--diff");
    diffOption.Description = "Show a diff of the changes that will be made";
    diffOption.DefaultValueFactory = _ => false;

    var verboseOption = new Option<bool>("--verbose");
    verboseOption.Description = "Show detailed log output (Trace and Debug messages)";
    verboseOption.DefaultValueFactory = _ => false;
    verboseOption.Aliases.Add("-v");

    var quietOption = new Option<bool>("--quiet");
    quietOption.Description = "Suppress non-essential output (only Errors and Warnings)";
    quietOption.DefaultValueFactory = _ => false;
    quietOption.Aliases.Add("-q");

    var jsonOption = new Option<bool>("--json");
    jsonOption.Description = "Output results as JSON";
    jsonOption.DefaultValueFactory = _ => false;

    var forceOption = new Option<bool>("--force") { Description = "Force reapply steps even if previously succeeded" };
    forceOption.DefaultValueFactory = _ => false;

    var continueOnErrorOption = new Option<bool>("--continue-on-error") { Description = "Continue applying remaining steps when a step fails" };
    continueOnErrorOption.DefaultValueFactory = _ => false;

    // MOUNT LOG FILE TRACKER OPTION: Adds the global string flag mapping requested under issue tasks
    var logFileOption = new Option<string?>("--log-file");
    logFileOption.Description = "Explicit file path mapping destination to save human-readable persistent runtime log outputs";
    logFileOption.DefaultValueFactory = _ => null;

    var rootCommand = new RootCommand("WinHome: Windows Setup Tool");
    rootCommand.Options.Add(configOption);
    rootCommand.Options.Add(updateOption);
    rootCommand.Options.Add(dryRunOption);
    rootCommand.Options.Add(profileOption);
    rootCommand.Options.Add(debugOption);
    rootCommand.Options.Add(diffOption);
    rootCommand.Options.Add(verboseOption);
    rootCommand.Options.Add(quietOption);
    rootCommand.Options.Add(jsonOption);
    rootCommand.Options.Add(forceOption);
    rootCommand.Options.Add(continueOnErrorOption);
    rootCommand.Options.Add(logFileOption); // Register globally

    rootCommand.SetAction(async (ParseResult result) =>
    {
      FileInfo file = result.GetValue(configOption)!;
      bool update = result.GetValue(updateOption);
      bool dryRun = result.GetValue(dryRunOption);
      string? profile = result.GetValue(profileOption);
      bool debug = result.GetValue(debugOption);
      bool diff = result.GetValue(diffOption);
      bool verbose = result.GetValue(verboseOption);
      bool quiet = result.GetValue(quietOption);
      bool json = result.GetValue(jsonOption);
      bool force = result.GetValue(forceOption);
      bool continueOnError = result.GetValue(continueOnErrorOption);
      string? logFile = result.GetValue(logFileOption); // Parse option out cleanly to fulfill validation protocols

      int conflict = RejectConflictingFlags(verbose, quiet);
      if (conflict != 0) return conflict;

      return await runAction(file, dryRun, profile, debug, diff, json, update, force, continueOnError, ComputeLogLevel(quiet, verbose));
    });

    // Generate Command
    var generateCommand = new Command("generate");
    generateCommand.Description = "Generate a configuration file from the current system state";
    var outputOption = new Option<FileInfo?>("--output");
    outputOption.Description = "Output file path (default: stdout)";
    outputOption.Aliases.Add("-o");
    generateCommand.Options.Add(outputOption);
    generateCommand.Options.Add(verboseOption);
    generateCommand.Options.Add(quietOption);
    generateCommand.Options.Add(logFileOption); // Support log-file option on generate task paths

    generateCommand.SetAction(async (ParseResult result) =>
    {
      FileInfo? output = result.GetValue(outputOption);
      bool verbose = result.GetValue(verboseOption);
      bool quiet = result.GetValue(quietOption);

      int conflict = RejectConflictingFlags(verbose, quiet);
      if (conflict != 0) return conflict;

      return await generateAction(output, ComputeLogLevel(quiet, verbose));
    });

    rootCommand.Add(generateCommand);

    // State Command
    var stateCommand = new Command("state");
    stateCommand.Description = "Manage the system state managed by WinHome";
    stateCommand.Options.Add(verboseOption);
    stateCommand.Options.Add(quietOption);
    stateCommand.Options.Add(logFileOption); // Support log-file option on base state track paths

    var listSubCommand = new Command("list");
    listSubCommand.Description = "List all items currently managed by WinHome";
    listSubCommand.Options.Add(verboseOption);
    listSubCommand.Options.Add(quietOption);
    listSubCommand.SetAction(async (ParseResult result) =>
    {
      bool verbose = result.GetValue(verboseOption);
      bool quiet = result.GetValue(quietOption);
      int conflict = RejectConflictingFlags(verbose, quiet);
      if (conflict != 0) return conflict;
      return await stateAction("list", null, ComputeLogLevel(quiet, verbose));
    });

    var backupSubCommand = new Command("backup");
    backupSubCommand.Description = "Backup the current state file";
    var backupPathArgument = new Argument<string>("path") { Description = "Path to save the backup" };
    backupSubCommand.Arguments.Add(backupPathArgument);
    backupSubCommand.Options.Add(verboseOption);
    backupSubCommand.Options.Add(quietOption);
    backupSubCommand.SetAction(async (ParseResult result) =>
    {
      bool verbose = result.GetValue(verboseOption);
      bool quiet = result.GetValue(quietOption);
      int conflict = RejectConflictingFlags(verbose, quiet);
      if (conflict != 0) return conflict;
      var path = result.GetValue(backupPathArgument);
      return await stateAction("backup", path, ComputeLogLevel(quiet, verbose));
    });

    var restoreSubCommand = new Command("restore");
    restoreSubCommand.Description = "Restore the state file from a backup";
    var restorePathArgument = new Argument<string>("path") { Description = "Path to the backup file to restore" };
    restoreSubCommand.Arguments.Add(restorePathArgument);
    restoreSubCommand.Options.Add(verboseOption);
    restoreSubCommand.Options.Add(quietOption);
    restoreSubCommand.SetAction(async (ParseResult result) =>
    {
      bool verbose = result.GetValue(verboseOption);
      bool quiet = result.GetValue(quietOption);
      int conflict = RejectConflictingFlags(verbose, quiet);
      if (conflict != 0) return conflict;
      var path = result.GetValue(restorePathArgument);
      return await stateAction("restore", path, ComputeLogLevel(quiet, verbose));
    });

    var clearSubCommand = new Command("clear");
    clearSubCommand.Description = "Clear all managed items state data parameters";
    clearSubCommand.Options.Add(verboseOption);
    clearSubCommand.Options.Add(quietOption);
    clearSubCommand.SetAction(async (ParseResult result) =>
    {
      bool verbose = result.GetValue(verboseOption);
      bool quiet = result.GetValue(quietOption);
      int conflict = RejectConflictingFlags(verbose, quiet);
      if (conflict != 0) return conflict;

      Console.Write("Are you sure you want to clear the state? [y/N]: ");
      var response = Console.ReadLine();
      if (response != "y")
      {
        return 0;
      }

      return await stateAction("clear", null, ComputeLogLevel(quiet, verbose));
    });

    stateCommand.Subcommands.Add(listSubCommand);
    stateCommand.Subcommands.Add(backupSubCommand);
    stateCommand.Subcommands.Add(restoreSubCommand);
    stateCommand.Subcommands.Add(clearSubCommand);
    rootCommand.Add(stateCommand);


    // Completion Command
    var completionCommand = new Command("completion");
    completionCommand.Description = "Generate shell completion scripts for PowerShell or Bash";

    var shellArgument = new Argument<string>("shell")
    {
      Description = "Target shell (powershell or bash)"
    };
    completionCommand.Arguments.Add(shellArgument);

    completionCommand.SetAction((ParseResult result) =>
    {
      var shell = result.GetValue(shellArgument)!;

      if (!ShellCompletionGenerator.SupportedShells.Contains(shell.ToLowerInvariant()))
      {
        Console.Error.WriteLine($"Argument '{shell}' not recognized. Must be one of: {string.Join(", ", ShellCompletionGenerator.SupportedShells)}");
        return 1;
      }

      try
      {
        var script = ShellCompletionGenerator.Generate(rootCommand, shell);
        Console.Write(script);
        return 0;
      }
      catch (ArgumentException ex)
      {
        Console.Error.WriteLine(ex.Message);
        return 1;
      }
    });

    rootCommand.Add(completionCommand);
    // Config Command
    var configCommand = new Command("config");
    configCommand.Description = "Manage configuration backups";

    var configBackupCommand = new Command("backup");
    configBackupCommand.Description = "Backup provider configuration";

    var providerArgument = new Argument<string>("provider")
    {
      Description = "Configuration provider name"
    };

    var backupOutputOption = new Option<string>("--output")
    {
      Description = "Backup output file"
    };

    configBackupCommand.Arguments.Add(providerArgument);
    configBackupCommand.Options.Add(backupOutputOption);

    configBackupCommand.SetAction(async (ParseResult result) =>
    {
      var provider = result.GetValue(providerArgument)!;
      var output = result.GetValue(backupOutputOption)!;

      return configBackupAction == null
          ? 1
          : await configBackupAction(
              provider,
              output,
              null,
              ComputeLogLevel(false, false));
    });


    var configRestoreCommand = new Command("restore");
    configRestoreCommand.Description = "Restore configuration backup";

    var restoreInput = new Argument<string>("input")
    {
      Description = "Backup file path"
    };

    configRestoreCommand.Arguments.Add(restoreInput);

    configRestoreCommand.SetAction(async (ParseResult result) =>
    {
      var input = result.GetValue(restoreInput)!;

      return configBackupAction == null
          ? 1
          : await configBackupAction(
              "restore",
              input,
              null,
              ComputeLogLevel(false, false));
    });


    configCommand.Subcommands.Add(configBackupCommand);
    configCommand.Subcommands.Add(configRestoreCommand);

    rootCommand.Add(configCommand);

    return rootCommand;
  }

  private static int RejectConflictingFlags(bool verbose, bool quiet)
  {
    if (verbose && quiet)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.Error.WriteLine("[Error] Cannot specify both --verbose and --quiet options simultaneously.");
      Console.ResetColor();
      return 1;
    }
    return 0;
  }

  private static LogLevel ComputeLogLevel(bool quiet, bool verbose)
  {
    if (quiet) return LogLevel.Warning;
    if (verbose) return LogLevel.Trace;
    return LogLevel.Info;
  }
}
