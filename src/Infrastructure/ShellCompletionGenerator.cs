using System.CommandLine;
using System.Text;
using System.Text.RegularExpressions;

namespace WinHome.Infrastructure;

/// <summary>
/// Generates shell completion scripts for PowerShell and Bash.
/// Dynamically walks the System.CommandLine tree to discover all options and subcommands.
/// </summary>
public static class ShellCompletionGenerator
{
  /// <summary>
  /// Generate a completion script for the given shell.
  /// </summary>
  /// <param name="rootCommand">The root command to generate completions for.</param>
  /// <param name="shell">Target shell: "powershell" or "bash".</param>
  /// <returns>The completion script as a string.</returns>
  /// <exception cref="ArgumentException">Thrown when shell is not supported.</exception>
  public static string Generate(RootCommand rootCommand, string shell)
  {
    if (rootCommand == null) throw new ArgumentNullException(nameof(rootCommand));

    return shell.ToLowerInvariant() switch
    {
      "powershell" or "pwsh" => GeneratePowerShell(rootCommand),
      "bash" => GenerateBash(rootCommand),
      _ => throw new ArgumentException(
          $"Unsupported shell: '{shell}'. Supported shells: powershell, bash.",
          nameof(shell))
    };
  }

  /// <summary>
  /// Returns the list of supported shell names.
  /// </summary>
  public static IReadOnlyList<string> SupportedShells => _supportedShells;

  private static readonly string[] _supportedShells = new[] { "powershell", "pwsh", "bash" };

  private static string GeneratePowerShell(RootCommand rootCommand)
  {
    var sb = new StringBuilder();
    sb.AppendLine("# WinHome CLI tab completion for PowerShell");
    sb.AppendLine("# Add this to your $PROFILE to enable tab completion.");
    sb.AppendLine("#");
    sb.AppendLine("# Usage:");
    sb.AppendLine("#   WinHome completion powershell | Out-String | Invoke-Expression");
    sb.AppendLine("#   # Or save to a file for persistence:");
    sb.AppendLine("#   WinHome completion powershell > ~/winhome-completion.ps1");
    sb.AppendLine("#   # and dot-source it in your profile: . ~/winhome-completion.ps1");
    sb.AppendLine();

    // Collect all completions from the command tree
    var completions = CollectCompletions(rootCommand, Regex.Escape("WinHome"));

    // Build the completer script block
    var scriptBlock = new StringBuilder();
    scriptBlock.AppendLine("    param($wordToComplete, $commandAst, $cursorPosition)");
    scriptBlock.AppendLine();
    scriptBlock.AppendLine("    $commands = @(");

    foreach (var (context, name, description) in completions)
    {
      var escapedDesc = description.Replace("'", "''");
      var escapedName = name.Replace("'", "''");
      var escapedContext = context.Replace("'", "''"); // context is already regex-escaped, just escape quotes
      scriptBlock.AppendLine($"        @{{ Context = '{escapedContext}'; Name = '{escapedName}'; Description = '{escapedDesc}' }}");
    }

    scriptBlock.AppendLine("    )");
    scriptBlock.AppendLine();
    scriptBlock.AppendLine("    $commandLine = $commandAst.ToString()");
    scriptBlock.AppendLine();
    scriptBlock.AppendLine("    $filteredCommands = $commands | Where-Object {");
    scriptBlock.AppendLine("        ($commandLine -match $_.Context -or $_.Context -eq 'WinHome') -and");
    scriptBlock.AppendLine("        $_.Name -like \"$wordToComplete*\"");
    scriptBlock.AppendLine("    }");
    scriptBlock.AppendLine();
    scriptBlock.AppendLine("    $filteredCommands | ForEach-Object {");
    scriptBlock.AppendLine("        [System.Management.Automation.CompletionResult]::new(");
    scriptBlock.AppendLine("            $_.Name,");
    scriptBlock.AppendLine("            $_.Name,");
    scriptBlock.AppendLine("            'ParameterValue',");
    scriptBlock.AppendLine("            $_.Description");
    scriptBlock.AppendLine("        )");
    scriptBlock.AppendLine("    }");

    // Register for both WinHome and winhome for case-insensitive matching
    sb.AppendLine("$_winhomeCompleter = {");
    sb.Append(scriptBlock);
    sb.AppendLine("}");
    sb.AppendLine();
    sb.AppendLine("Register-ArgumentCompleter -Native -CommandName WinHome -ScriptBlock $_winhomeCompleter");
    sb.AppendLine("Register-ArgumentCompleter -Native -CommandName winhome -ScriptBlock $_winhomeCompleter");

    return sb.ToString();
  }

  private static string GenerateBash(RootCommand rootCommand)
  {
    var sb = new StringBuilder();
    sb.AppendLine("#!/bin/bash");
    sb.AppendLine("# WinHome CLI tab completion for Bash");
    sb.AppendLine("# Add this to your ~/.bashrc or ~/.bash_profile to enable tab completion.");
    sb.AppendLine("#");
    sb.AppendLine("# Usage:");
    sb.AppendLine("#   eval \"$(WinHome completion bash)\"");
    sb.AppendLine("#   # Or save to a file for persistence:");
    sb.AppendLine("#   WinHome completion bash > ~/.winhome-completion.bash");
    sb.AppendLine("#   echo \"source ~/.winhome-completion.bash\" >> ~/.bashrc");
    sb.AppendLine();

    // Collect all top-level options and subcommands
    var globalOptions = CollectOptions(rootCommand);
    var subcommands = CollectSubcommands(rootCommand);

    sb.AppendLine("_winhome_completions() {");
    sb.AppendLine("    local cur prev words cword");
    sb.AppendLine("    _init_completion || return");
    sb.AppendLine();

    // Build case statement for file path completion on options that accept file paths
    sb.AppendLine("    case \"${prev}\" in");
    sb.AppendLine("        --config)");
    sb.AppendLine("            _filedir '@(yaml|yml)'");
    sb.AppendLine("            return");
    sb.AppendLine("            ;;");
    sb.AppendLine("        --output|-o)");
    sb.AppendLine("            _filedir '@(yaml|yml)'");
    sb.AppendLine("            return");
    sb.AppendLine("            ;;");
    sb.AppendLine("    esac");
    sb.AppendLine();

    // Context-aware subcommand completions — dynamically generated from the command tree
    sb.AppendLine("    # Context-aware completion based on previous words");
    sb.AppendLine("    local i");
    sb.AppendLine("    for (( i=1; i < cword; i++ )); do");
    sb.AppendLine("        case \"${words[i]}\" in");

    // Dynamically iterate over all top-level subcommands
    foreach (var sub in rootCommand.Subcommands)
    {
      var subSubs = CollectSubcommands(sub);
      var subOpts = CollectOptions(sub);
      var allCompletions = subSubs.Concat(subOpts).ToList();

      sb.AppendLine($"            {sub.Name})");

      if (allCompletions.Count > 0)
      {
        sb.Append("                COMPREPLY=($(compgen -W \"");
        sb.Append(string.Join(" ", allCompletions));
        sb.AppendLine("\" -- \"${cur}\"))");
      }

      sb.AppendLine("                return");
      sb.AppendLine("                ;;");

      // Also handle nested subcommands that need file path completion (e.g. backup, restore)
      var filePathSubs = sub.Subcommands
          .Where(s => s.Arguments.Any())
          .Select(s => s.Name)
          .ToList();

      if (filePathSubs.Count > 0)
      {
        sb.AppendLine($"            {string.Join("|", filePathSubs)})");
        sb.AppendLine("                _filedir");
        sb.AppendLine("                return");
        sb.AppendLine("                ;;");
      }
    }

    sb.AppendLine("        esac");
    sb.AppendLine("    done");
    sb.AppendLine();

    // Default: root-level completions
    sb.Append("    COMPREPLY=($(compgen -W \"");
    sb.Append(string.Join(" ", subcommands.Concat(globalOptions)));
    sb.AppendLine("\" -- \"${cur}\"))");
    sb.AppendLine("}");
    sb.AppendLine();
    sb.AppendLine("complete -F _winhome_completions WinHome");
    sb.AppendLine("complete -F _winhome_completions winhome");

    return sb.ToString();
  }

  /// <summary>
  /// Collects all completions (options and subcommands) with their context path.
  /// Context paths use regex-escaped literals for safe -match usage in PowerShell.
  /// </summary>
  private static List<(string Context, string Name, string Description)> CollectCompletions(
      Command command, string contextPath)
  {
    var results = new List<(string Context, string Name, string Description)>();

    // Add options for this command
    foreach (var option in command.Options)
    {
      results.Add((contextPath, option.Name, option.Description ?? option.Name));
      foreach (var alias in option.Aliases)
      {
        if (alias != option.Name)
        {
          results.Add((contextPath, alias, option.Description ?? alias));
        }
      }
    }

    // Add subcommands
    foreach (var sub in command.Subcommands)
    {
      results.Add((contextPath, sub.Name, sub.Description ?? sub.Name));
      // Recurse into subcommands with a regex-safe context separator
      results.AddRange(CollectCompletions(sub, $@"{contextPath}\s+{Regex.Escape(sub.Name)}"));
    }

    return results;
  }

  /// <summary>Collects option names (including aliases) for a command.</summary>
  private static List<string> CollectOptions(Command command)
  {
    var options = new List<string>();
    foreach (var option in command.Options)
    {
      options.Add(option.Name);
      foreach (var alias in option.Aliases)
      {
        if (alias != option.Name)
          options.Add(alias);
      }
    }
    return options;
  }

  /// <summary>Collects subcommand names for a command.</summary>
  private static List<string> CollectSubcommands(Command command)
  {
    return command.Subcommands.Select(s => s.Name).ToList();
  }
}
