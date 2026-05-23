using System.CommandLine;
using WinHome.Infrastructure;
using Xunit;

namespace WinHome.Tests;

public class ShellCompletionGeneratorTests
{
    /// <summary>Helper to build a minimal RootCommand for targeted edge-case tests.</summary>
    private static RootCommand BuildTestRootCommand()
    {
        var rootCommand = new RootCommand("WinHome: Windows Setup Tool");

        rootCommand.Options.Add(new Option<bool>("--dry-run") { Description = "Preview changes without applying them" });
        rootCommand.Options.Add(new Option<string>("--config") { Description = "Path to the YAML configuration file" });
        rootCommand.Options.Add(new Option<string?>("--profile") { Description = "Activate a specific profile" });
        rootCommand.Options.Add(new Option<bool>("--debug") { Description = "Show detailed error information" });
        rootCommand.Options.Add(new Option<bool>("--diff") { Description = "Show a diff of the changes" });
        rootCommand.Options.Add(new Option<bool>("--verbose") { Description = "Show detailed log output" });
        rootCommand.Options.Add(new Option<bool>("--quiet") { Description = "Suppress non-essential output" });
        rootCommand.Options.Add(new Option<bool>("--json") { Description = "Output results as JSON" });
        rootCommand.Options.Add(new Option<bool>("--update") { Description = "Check for updates" });

        var stateCommand = new Command("state") { Description = "Manage state" };
        stateCommand.Subcommands.Add(new Command("list") { Description = "List managed items" });
        stateCommand.Subcommands.Add(new Command("backup") { Description = "Backup state" });
        stateCommand.Subcommands.Add(new Command("restore") { Description = "Restore state" });
        rootCommand.Add(stateCommand);

        var generateCommand = new Command("generate") { Description = "Generate configuration" };
        generateCommand.Options.Add(new Option<string?>("--output") { Description = "Output file path" });
        rootCommand.Add(generateCommand);

        return rootCommand;
    }

    /// <summary>
    /// Build the real RootCommand from CliBuilder to validate against the actual CLI surface.
    /// Uses stub callbacks since we only need the command tree structure for completion generation.
    /// </summary>
    private static RootCommand BuildRealRootCommand()
    {
        return CliBuilder.BuildRootCommand(
            runAction: (file, dryRun, profile, debug, diff, json, update, logLevel) => Task.FromResult(0),
            generateAction: (output, logLevel) => Task.FromResult(0),
            stateAction: (command, path, logLevel) => Task.FromResult(0)
        );
    }

    #region Edge-Case Tests (using mock RootCommand)

    [Fact]
    public void Generate_PowerShell_ReturnsNonEmptyScript()
    {
        var root = BuildTestRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "powershell");

        Assert.False(string.IsNullOrWhiteSpace(script));
    }

    [Fact]
    public void Generate_Bash_ReturnsNonEmptyScript()
    {
        var root = BuildTestRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "bash");

        Assert.False(string.IsNullOrWhiteSpace(script));
    }

    [Fact]
    public void Generate_Pwsh_WorksAsPowerShellAlias()
    {
        var root = BuildTestRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "pwsh");

        Assert.Contains("Register-ArgumentCompleter", script);
    }

    [Fact]
    public void Generate_UnsupportedShell_ThrowsArgumentException()
    {
        var root = BuildTestRootCommand();

        var ex = Assert.Throws<ArgumentException>(() =>
            ShellCompletionGenerator.Generate(root, "fish"));

        Assert.Contains("Unsupported shell", ex.Message);
        Assert.Contains("fish", ex.Message);
    }

    [Fact]
    public void Generate_CaseInsensitive_WorksForPowerShell()
    {
        var root = BuildTestRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "PowerShell");

        Assert.Contains("Register-ArgumentCompleter", script);
    }

    [Fact]
    public void Generate_CaseInsensitive_WorksForBash()
    {
        var root = BuildTestRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "BASH");

        Assert.Contains("complete -F _winhome_completions WinHome", script);
    }

    [Fact]
    public void SupportedShells_ContainsPowerShellAndBash()
    {
        var shells = ShellCompletionGenerator.SupportedShells;

        Assert.Contains("powershell", shells);
        Assert.Contains("bash", shells);
    }

    #endregion

    #region Integration Tests (using real CliBuilder RootCommand)

    [Fact]
    public void RealCli_PowerShell_ContainsRegisterArgumentCompleter()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "powershell");

        Assert.Contains("Register-ArgumentCompleter", script);
    }

    [Fact]
    public void RealCli_PowerShell_RegistersBothCasings()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "powershell");

        Assert.Contains("Register-ArgumentCompleter -Native -CommandName WinHome", script);
        Assert.Contains("Register-ArgumentCompleter -Native -CommandName winhome", script);
    }

    [Fact]
    public void RealCli_PowerShell_ContainsAllGlobalOptions()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "powershell");

        Assert.Contains("--dry-run", script);
        Assert.Contains("--config", script);
        Assert.Contains("--profile", script);
        Assert.Contains("--debug", script);
        Assert.Contains("--diff", script);
        Assert.Contains("--verbose", script);
        Assert.Contains("--quiet", script);
        Assert.Contains("--json", script);
        Assert.Contains("--update", script);
    }

    [Fact]
    public void RealCli_PowerShell_ContainsSubcommands()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "powershell");

        Assert.Contains("state", script);
        Assert.Contains("generate", script);
        Assert.Contains("completion", script);
    }

    [Fact]
    public void RealCli_PowerShell_ContainsStateSubcommands()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "powershell");

        Assert.Contains("list", script);
        Assert.Contains("backup", script);
        Assert.Contains("restore", script);
    }

    [Fact]
    public void RealCli_Bash_ContainsCompleteCommand()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "bash");

        Assert.Contains("complete -F _winhome_completions WinHome", script);
        Assert.Contains("complete -F _winhome_completions winhome", script);
    }

    [Fact]
    public void RealCli_Bash_ContainsAllGlobalOptions()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "bash");

        Assert.Contains("--dry-run", script);
        Assert.Contains("--config", script);
        Assert.Contains("--profile", script);
        Assert.Contains("--debug", script);
        Assert.Contains("--diff", script);
        Assert.Contains("--verbose", script);
        Assert.Contains("--quiet", script);
        Assert.Contains("--json", script);
        Assert.Contains("--update", script);
    }

    [Fact]
    public void RealCli_Bash_ContainsContextAwareStateCompletions()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "bash");

        Assert.Contains("state)", script);
        Assert.Contains("list", script);
        Assert.Contains("backup", script);
        Assert.Contains("restore", script);
    }

    [Fact]
    public void RealCli_Bash_ContainsFilePathCompletion()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "bash");

        Assert.Contains("--config)", script);
        Assert.Contains("_filedir", script);
    }

    [Fact]
    public void RealCli_Bash_DynamicallyIncludesAllSubcommands()
    {
        var root = BuildRealRootCommand();
        var script = ShellCompletionGenerator.Generate(root, "bash");

        // All top-level subcommands should appear in root-level completions
        foreach (var sub in root.Subcommands)
        {
            Assert.Contains(sub.Name, script);
        }
    }

    #endregion
}
