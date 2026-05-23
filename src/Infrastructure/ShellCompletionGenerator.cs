using System.CommandLine;
using System.Text;

namespace WinHome.Infrastructure;

/// <summary>
/// Generates shell completion scripts for PowerShell and Bash.
/// Walks the System.CommandLine tree to discover all options and subcommands.
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
    public static IReadOnlyList<string> SupportedShells => new[] { "powershell", "bash" };

    private static string GeneratePowerShell(RootCommand rootCommand)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# WinHome CLI tab completion for PowerShell");
        sb.AppendLine("# Add this to your $PROFILE to enable tab completion.");
        sb.AppendLine("#");
        sb.AppendLine("# Usage:");
        sb.AppendLine("#   WinHome completion powershell | Out-String | Invoke-Expression");
        sb.AppendLine("#   # Or add to profile for persistence:");
        sb.AppendLine("#   WinHome completion powershell >> $PROFILE");
        sb.AppendLine();
        sb.AppendLine("Register-ArgumentCompleter -Native -CommandName WinHome -ScriptBlock {");
        sb.AppendLine("    param($wordToComplete, $commandAst, $cursorPosition)");
        sb.AppendLine();

        // Collect all completions from the command tree
        var completions = CollectCompletions(rootCommand, "WinHome");

        sb.AppendLine("    $commands = @(");

        foreach (var (context, name, description) in completions)
        {
            var escapedDesc = description.Replace("'", "''");
            var escapedName = name.Replace("'", "''");
            sb.AppendLine($"        @{{ Context = '{context}'; Name = '{escapedName}'; Description = '{escapedDesc}' }}");
        }

        sb.AppendLine("    )");
        sb.AppendLine();
        sb.AppendLine("    $commandLine = $commandAst.ToString()");
        sb.AppendLine();
        sb.AppendLine("    $filteredCommands = $commands | Where-Object {");
        sb.AppendLine("        ($commandLine -match $_.Context -or $_.Context -eq 'WinHome') -and");
        sb.AppendLine("        $_.Name -like \"$wordToComplete*\"");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    $filteredCommands | ForEach-Object {");
        sb.AppendLine("        [System.Management.Automation.CompletionResult]::new(");
        sb.AppendLine("            $_.Name,");
        sb.AppendLine("            $_.Name,");
        sb.AppendLine("            'ParameterValue',");
        sb.AppendLine("            $_.Description");
        sb.AppendLine("        )");
        sb.AppendLine("    }");
        sb.AppendLine("}");

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
        sb.AppendLine("#   # Or add to profile for persistence:");
        sb.AppendLine("#   WinHome completion bash >> ~/.bashrc");
        sb.AppendLine();

        // Collect all top-level options and subcommands
        var globalOptions = CollectOptions(rootCommand);
        var subcommands = CollectSubcommands(rootCommand);

        sb.AppendLine("_winhome_completions() {");
        sb.AppendLine("    local cur prev words cword");
        sb.AppendLine("    _init_completion || return");
        sb.AppendLine();

        // Build case statement for context-aware completion
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

        // Context-aware subcommand completions
        sb.AppendLine("    # Context-aware completion based on previous words");
        sb.AppendLine("    local i");
        sb.AppendLine("    for (( i=1; i < cword; i++ )); do");
        sb.AppendLine("        case \"${words[i]}\" in");

        // state subcommands
        sb.AppendLine("            state)");
        sb.Append("                COMPREPLY=($(compgen -W \"");
        var stateCmd = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "state");
        if (stateCmd != null)
        {
            var stateSubs = CollectSubcommands(stateCmd);
            var stateOpts = CollectOptions(stateCmd);
            sb.Append(string.Join(" ", stateSubs.Concat(stateOpts)));
        }
        sb.AppendLine("\" -- \"${cur}\"))");
        sb.AppendLine("                return");
        sb.AppendLine("                ;;");

        // generate subcommand options
        sb.AppendLine("            generate)");
        sb.Append("                COMPREPLY=($(compgen -W \"");
        var genCmd = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "generate");
        if (genCmd != null)
        {
            var genOpts = CollectOptions(genCmd);
            sb.Append(string.Join(" ", genOpts));
        }
        sb.AppendLine("\" -- \"${cur}\"))");
        sb.AppendLine("                return");
        sb.AppendLine("                ;;");

        // state backup/restore need file path completion
        sb.AppendLine("            backup|restore)");
        sb.AppendLine("                _filedir");
        sb.AppendLine("                return");
        sb.AppendLine("                ;;");

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
            // Recurse into subcommands
            results.AddRange(CollectCompletions(sub, $"{contextPath}.*{sub.Name}"));
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
