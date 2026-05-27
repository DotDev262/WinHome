using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using WinHome.Infrastructure;
using WinHome.Interfaces;
using Xunit;

namespace WinHome.Tests;

public class CliBuilderTests
{
  private static Func<FileInfo, bool, string?, bool, bool, bool, bool, bool, bool, LogLevel, Task<int>> NoOpRunAction()
  {
    return (_, _, _, _, _, _, _, _, _, _) => Task.FromResult(0);
  }

  private static RootCommand BuildRootCommand(
      Func<FileInfo, bool, string?, bool, bool, bool, bool, bool, bool, LogLevel, Task<int>> runAction,
      Func<FileInfo?, LogLevel, Task<int>>? generateAction = null,
      Func<string, string?, LogLevel, Task<int>>? stateAction = null)
  {
    return CliBuilder.BuildRootCommand(
        runAction: runAction,
        generateAction: generateAction ?? ((output, level) => Task.FromResult(0)),
        stateAction: stateAction ?? ((command, path, level) => Task.FromResult(0))
    );
  }

  [Fact]
  public async Task RootCommand_BindsOptions()
  {
    // Arrange
    FileInfo? capturedFile = null;
    bool capturedDryRun = false;
    string? capturedProfile = null;
    bool capturedDebug = false;
    bool capturedDiff = false;
    bool capturedJson = false;
    bool capturedUpdate = false;
    bool capturedForce = false;
    bool capturedContinueOnError = false;
    LogLevel capturedLevel = LogLevel.Info;

    var root = BuildRootCommand((file, dryRun, profile, debug, diff, json, update, force, continueOnError, level) =>
    {
      capturedFile = file;
      capturedDryRun = dryRun;
      capturedProfile = profile;
      capturedDebug = debug;
      capturedDiff = diff;
      capturedJson = json;
      capturedUpdate = update;
      capturedForce = force;
      capturedContinueOnError = continueOnError;
      capturedLevel = level;
      return Task.FromResult(0);
    });

    // Act
    var exitCode = await root.Parse(new[]
    {
            "--config", "custom.yaml",
            "--dry-run",
            "--profile", "work",
            "--debug",
            "--diff",
            "--json",
            "--update",
            "--force",
            "--continue-on-error",
            "--verbose"
        }).InvokeAsync();

    // Assert
    Assert.Equal(0, exitCode);
    Assert.NotNull(capturedFile);
    Assert.EndsWith($"{Path.DirectorySeparatorChar}custom.yaml", capturedFile.FullName);
    Assert.True(capturedDryRun);
    Assert.Equal("work", capturedProfile);
    Assert.True(capturedDebug);
    Assert.True(capturedDiff);
    Assert.True(capturedJson);
    Assert.True(capturedUpdate);
    Assert.True(capturedForce);
    Assert.True(capturedContinueOnError);
    Assert.Equal(LogLevel.Trace, capturedLevel);
  }

  [Fact]
  public async Task RootCommand_BindsAliasOptions()
  {
    // Arrange
    bool capturedDryRun = false;
    string? capturedProfile = null;
    bool capturedUpdate = false;

    var root = BuildRootCommand((_, dryRun, profile, _, _, _, update, _, _, _) =>
    {
      capturedDryRun = dryRun;
      capturedProfile = profile;
      capturedUpdate = update;
      return Task.FromResult(0);
    });

    // Act
    var exitCode = await root.Parse(new[] { "-d", "-p", "dev", "-u" }).InvokeAsync();

    // Assert
    Assert.Equal(0, exitCode);
    Assert.True(capturedDryRun);
    Assert.Equal("dev", capturedProfile);
    Assert.True(capturedUpdate);
  }

  [Theory]
  [InlineData("--verbose", LogLevel.Trace)]
  [InlineData("--quiet", LogLevel.Warning)]
  [InlineData("-v", LogLevel.Trace)]
  [InlineData("-q", LogLevel.Warning)]
  public async Task RootCommand_LogLevel_Mapping(string option, LogLevel expected)
  {
    // Arrange
    LogLevel capturedLevel = LogLevel.Info;

    var root = BuildRootCommand((_, _, _, _, _, _, _, _, _, level) =>
    {
      capturedLevel = level;
      return Task.FromResult(0);
    });

    // Act
    var exitCode = await root.Parse(new[] { option }).InvokeAsync();

    // Assert
    Assert.Equal(0, exitCode);
    Assert.Equal(expected, capturedLevel);
  }

  [Fact]
  public async Task RootCommand_DefaultsToFalseAndInfo()
  {
    // Arrange
    bool capturedDryRun = true;
    string? capturedProfile = "value";
    bool capturedDebug = true;
    bool capturedDiff = true;
    bool capturedJson = true;
    bool capturedUpdate = true;
    bool capturedForce = true;
    bool capturedContinueOnError = true;
    LogLevel capturedLevel = LogLevel.Trace;

    var root = BuildRootCommand((file, dryRun, profile, debug, diff, json, update, force, continueOnError, level) =>
    {
      capturedDryRun = dryRun;
      capturedProfile = profile;
      capturedDebug = debug;
      capturedDiff = diff;
      capturedJson = json;
      capturedUpdate = update;
      capturedForce = force;
      capturedContinueOnError = continueOnError;
      capturedLevel = level;
      return Task.FromResult(0);
    });

    // Act
    var exitCode = await root.Parse(Array.Empty<string>()).InvokeAsync();

    // Assert
    Assert.Equal(0, exitCode);
    Assert.False(capturedDryRun);
    Assert.Null(capturedProfile);
    Assert.False(capturedDebug);
    Assert.False(capturedDiff);
    Assert.False(capturedJson);
    Assert.False(capturedUpdate);
    Assert.False(capturedForce);
    Assert.False(capturedContinueOnError);
    Assert.Equal(LogLevel.Info, capturedLevel);
  }

  [Fact]
  public async Task RootCommand_DefaultConfig_UsesEnvPath()
  {
    // Arrange
    var original = Environment.GetEnvironmentVariable("WINHOME_CONFIG_PATH");
    try
    {
      Environment.SetEnvironmentVariable("WINHOME_CONFIG_PATH", "env-config.yaml");
      FileInfo? capturedFile = null;

      var root = BuildRootCommand((file, _, _, _, _, _, _, _, _, _) =>
      {
        capturedFile = file;
        return Task.FromResult(0);
      });

      var exitCode = await root.Parse(Array.Empty<string>()).InvokeAsync();

      // Assert
      Assert.Equal(0, exitCode);
      Assert.NotNull(capturedFile);
      Assert.EndsWith($"{Path.DirectorySeparatorChar}env-config.yaml", capturedFile.FullName);
    }
    finally
    {
      Environment.SetEnvironmentVariable("WINHOME_CONFIG_PATH", original);
    }
  }

  [Fact]
  public async Task RootCommand_DefaultConfig_FallsBackToConfigYaml()
  {
    // Arrange
    var original = Environment.GetEnvironmentVariable("WINHOME_CONFIG_PATH");
    try
    {
      Environment.SetEnvironmentVariable("WINHOME_CONFIG_PATH", null);
      FileInfo? capturedFile = null;

      var root = BuildRootCommand((file, _, _, _, _, _, _, _, _, _) =>
      {
        capturedFile = file;
        return Task.FromResult(0);
      });

      var exitCode = await root.Parse(Array.Empty<string>()).InvokeAsync();

      // Assert
      Assert.Equal(0, exitCode);
      Assert.NotNull(capturedFile);
      Assert.EndsWith($"{Path.DirectorySeparatorChar}config.yaml", capturedFile.FullName);
    }
    finally
    {
      Environment.SetEnvironmentVariable("WINHOME_CONFIG_PATH", original);
    }
  }

  [Fact]
  public async Task RootCommand_RejectsVerboseAndQuiet()
  {
    // Arrange
    bool runCalled = false;

    var root = BuildRootCommand((_, _, _, _, _, _, _, _, _, _) =>
    {
      runCalled = true;
      return Task.FromResult(0);
    });

    // Act
    var exitCode = await root.Parse(new[] { "--verbose", "--quiet" }).InvokeAsync();

    // Assert
    Assert.NotEqual(0, exitCode);
    Assert.False(runCalled);
  }

  [Fact]
  public async Task RootCommand_InvalidOption_ReturnsNonZero()
  {
    // Arrange
    var root = BuildRootCommand(NoOpRunAction());

    // Act
    var exitCode = await root.Parse(new[] { "--invalid-option" }).InvokeAsync();

    // Assert
    Assert.NotEqual(0, exitCode);
  }

  [Fact]
  public async Task RootCommand_MissingProfileValue_ReturnsNonZero()
  {
    // Arrange
    var root = BuildRootCommand(NoOpRunAction());

    // Act
    var exitCode = await root.Parse(new[] { "--profile" }).InvokeAsync();

    // Assert
    Assert.NotEqual(0, exitCode);
  }

  [Fact]
  public async Task GenerateCommand_BindsOutputAndLogLevel()
  {
    // Arrange
    FileInfo? capturedOutput = null;
    LogLevel capturedLevel = LogLevel.Info;

    var root = BuildRootCommand(
        runAction: NoOpRunAction(),
        generateAction: (output, level) =>
        {
          capturedOutput = output;
          capturedLevel = level;
          return Task.FromResult(0);
        });

    // Act
    var exitCode = await root.Parse(new[] { "generate", "--output", "out.yaml", "--quiet" }).InvokeAsync();

    // Assert
    Assert.Equal(0, exitCode);
    Assert.NotNull(capturedOutput);
    Assert.EndsWith($"{Path.DirectorySeparatorChar}out.yaml", capturedOutput.FullName);
    Assert.Equal(LogLevel.Warning, capturedLevel);
  }

  [Fact]
  public async Task GenerateCommand_DefaultLogLevel_IsInfo()
  {
    // Arrange
    LogLevel capturedLevel = LogLevel.Trace;
    FileInfo? capturedOutput = new FileInfo("placeholder");

    var root = BuildRootCommand(
        runAction: NoOpRunAction(),
        generateAction: (output, level) =>
        {
          capturedOutput = output;
          capturedLevel = level;
          return Task.FromResult(0);
        });

    // Act
    var exitCode = await root.Parse(new[] { "generate" }).InvokeAsync();

    // Assert
    Assert.Equal(0, exitCode);
    Assert.Null(capturedOutput);
    Assert.Equal(LogLevel.Info, capturedLevel);
  }

  [Fact]
  public async Task GenerateCommand_BindsOutputAlias()
  {
    // Arrange
    FileInfo? capturedOutput = null;

    var root = BuildRootCommand(
        runAction: NoOpRunAction(),
        generateAction: (output, level) =>
        {
          capturedOutput = output;
          return Task.FromResult(0);
        });

    // Act
    var exitCode = await root.Parse(new[] { "generate", "-o", "alias.yaml" }).InvokeAsync();

    // Assert
    Assert.Equal(0, exitCode);
    Assert.NotNull(capturedOutput);
    Assert.EndsWith($"{Path.DirectorySeparatorChar}alias.yaml", capturedOutput.FullName);
  }

  [Fact]
  public async Task GenerateCommand_RejectsVerboseAndQuiet()
  {
    // Arrange
    bool generateCalled = false;

    var root = BuildRootCommand(
        runAction: NoOpRunAction(),
        generateAction: (output, level) =>
        {
          generateCalled = true;
          return Task.FromResult(0);
        });

    // Act
    var exitCode = await root.Parse(new[] { "generate", "--verbose", "--quiet" }).InvokeAsync();

    // Assert
    Assert.NotEqual(0, exitCode);
    Assert.False(generateCalled);
  }

  [Fact]
  public async Task StateCommand_BindsArgsAndLogLevel()
  {
    // Arrange
    string? capturedCommand = null;
    string? capturedPath = "placeholder";
    LogLevel capturedLevel = LogLevel.Info;

    var root = BuildRootCommand(
        runAction: NoOpRunAction(),
        stateAction: (command, path, level) =>
        {
          capturedCommand = command;
          capturedPath = path;
          capturedLevel = level;
          return Task.FromResult(0);
        });

    // Act
    var exitCode = await root.Parse(new[] { "state", "list", "--verbose" }).InvokeAsync();

    // Assert
    Assert.Equal(0, exitCode);
    Assert.Equal("list", capturedCommand);
    Assert.Null(capturedPath);
    Assert.Equal(LogLevel.Trace, capturedLevel);
  }

  [Fact]
  public async Task CompletionCommand_KnownShell_ReturnsZero()
  {
    // Arrange
    var root = BuildRootCommand(NoOpRunAction());
    int exitCode;
    string output;

    // Act
    using (var consoleInterceptor = new ConsoleOutputInterceptor())
    {
      exitCode = await root.Parse(new[] { "completion", "powershell" }).InvokeAsync();
      output = consoleInterceptor.GetOutput();
    }

    // Assert
    Assert.Equal(0, exitCode);
    Assert.Contains("Register-ArgumentCompleter", output);
  }

  [Fact]
  public async Task CompletionCommand_UnknownShell_ReturnsOne()
  {
    // Arrange
    var root = BuildRootCommand(NoOpRunAction());

    // Act
    var exitCode = await root.Parse(new[] { "completion", "fish" }).InvokeAsync();

    // Assert
    Assert.NotEqual(0, exitCode);
  }

  [Fact]
  public async Task CompletionCommand_MissingShell_ReturnsNonZero()
  {
    // Arrange
    var root = BuildRootCommand(NoOpRunAction());

    // Act
    var exitCode = await root.Parse(new[] { "completion" }).InvokeAsync();

    // Assert
    Assert.NotEqual(0, exitCode);
  }
}
