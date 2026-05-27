using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using WinHome.Infrastructure;
using WinHome.Interfaces;
using WinHome.Models;
using Xunit;

namespace WinHome.Tests;

public class StateCommandTests
{
    private readonly Mock<IStateService> _mockStateService;

    public StateCommandTests()
    {
        _mockStateService = new Mock<IStateService>();
    }

    private RootCommand BuildRealCommand(Func<string, string?, LogLevel, Task<int>> stateAction)
    {
        return CliBuilder.BuildRootCommand(
            runAction: (file, dryRun, profile, debug, diff, json, update, forceReapply, continueOnError, logLevel) => Task.FromResult(0),
            generateAction: (output, logLevel) => Task.FromResult(0),
            stateAction: stateAction
        );
    }

    [Fact]
    public async Task StateList_PrintsManagedItems()
    {
        // Arrange
        var items = new HashSet<string> { "git", "vscode", "winget" };
        _mockStateService.Setup(s => s.ListItems()).Returns(items);

        string? capturedAction = null;
        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            capturedAction = action;
            var result = _mockStateService.Object.ListItems();
            foreach (var item in result)
                Console.WriteLine(item);
            return result.Any() ? 0 : 1;
        });

        int exitCode;
        string output;

        // Act & Assert safely using the Disposable wrapper
        using (var consoleInterceptor = new ConsoleOutputInterceptor())
        {
            exitCode = await root.Parse(new[] { "state", "list" }).InvokeAsync();
            output = consoleInterceptor.GetOutput();
        }

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Equal("list", capturedAction);
        Assert.Contains("git", output);
        Assert.Contains("vscode", output);
        Assert.Contains("winget", output);
        _mockStateService.Verify(s => s.ListItems(), Times.Once);
    }

    [Fact]
    public async Task StateList_WhenEmpty_ReturnsOne()
    {
        // Arrange
        _mockStateService.Setup(s => s.ListItems()).Returns(new HashSet<string>());

        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            var result = _mockStateService.Object.ListItems();
            foreach (var item in result)
                Console.WriteLine(item);
            return result.Any() ? 0 : 1;
        });

        int exitCode;
        string output;

        // Act
        using (var consoleInterceptor = new ConsoleOutputInterceptor())
        {
            exitCode = await root.Parse(new[] { "state", "list" }).InvokeAsync();
            output = consoleInterceptor.GetOutput();
        }

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Empty(output.Trim());
    }

    [Fact]
    public async Task StateBackup_CallsBackupState_WithCorrectPath()
    {
        string targetPath = "backup.json";
        string? capturedPath = null;
        _mockStateService.Setup(s => s.BackupState(targetPath));

        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            if (action == "backup" && path != null)
            {
                capturedPath = path;
                _mockStateService.Object.BackupState(path);
                return 0;
            }
            return 1;
        });

        int exitCode = await root.Parse(new[] { "state", "backup", targetPath }).InvokeAsync();

        Assert.Equal(0, exitCode);
        Assert.Equal(targetPath, capturedPath);
        _mockStateService.Verify(s => s.BackupState(targetPath), Times.Once);
    }

    [Fact]
    public async Task StateBackup_PermissionsError_ReturnsFailure()
    {
        string restrictedPath = "C:\\Windows\\System32\\backup.json";
        _mockStateService
            .Setup(s => s.BackupState(restrictedPath))
            .Throws(new UnauthorizedAccessException("Access denied"));

        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            try
            {
                if (action == "backup" && path != null)
                {
                    _mockStateService.Object.BackupState(path);
                    return 0;
                }
                return 1;
            }
            catch (UnauthorizedAccessException)
            {
                return 1;
            }
        });

        int exitCode = await root.Parse(new[] { "state", "backup", restrictedPath }).InvokeAsync();

        Assert.Equal(1, exitCode);
        _mockStateService.Verify(s => s.BackupState(restrictedPath), Times.Once);
    }

    [Fact]
    public async Task StateRestore_RoundTrip_BackupThenRestore()
    {
        string backupPath = "round_trip.json";
        _mockStateService.Setup(s => s.BackupState(backupPath));
        _mockStateService.Setup(s => s.RestoreState(backupPath));

        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            if (path == null) return 1;
            if (action == "backup") { _mockStateService.Object.BackupState(path); return 0; }
            if (action == "restore") { _mockStateService.Object.RestoreState(path); return 0; }
            return 1;
        });

        int backupExit = await root.Parse(new[] { "state", "backup", backupPath }).InvokeAsync();
        int restoreExit = await root.Parse(new[] { "state", "restore", backupPath }).InvokeAsync();

        Assert.Equal(0, backupExit);
        Assert.Equal(0, restoreExit);
        _mockStateService.Verify(s => s.BackupState(backupPath), Times.Once);
        _mockStateService.Verify(s => s.RestoreState(backupPath), Times.Once);
    }

    [Fact]
    public async Task StateRestore_MissingFile_ReturnsFailure()
    {
        string missingPath = "non_existent.json";
        _mockStateService
            .Setup(s => s.RestoreState(missingPath))
            .Throws(new FileNotFoundException("Backup file not found"));

        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            try
            {
                if (action == "restore" && path != null)
                {
                    _mockStateService.Object.RestoreState(path);
                    return 0;
                }
                return 1;
            }
            catch (FileNotFoundException)
            {
                return 1;
            }
        });

        int exitCode = await root.Parse(new[] { "state", "restore", missingPath }).InvokeAsync();

        Assert.Equal(1, exitCode);
        _mockStateService.Verify(s => s.RestoreState(missingPath), Times.Once);
    }

    [Fact]
    public async Task StateRestore_CorruptFile_ReturnsFailure()
    {
        string corruptPath = "corrupt.json";
        _mockStateService
            .Setup(s => s.RestoreState(corruptPath))
            .Throws(new InvalidDataException("Corrupted state"));

        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            try
            {
                if (action == "restore" && path != null)
                {
                    _mockStateService.Object.RestoreState(path);
                    return 0;
                }
                return 1;
            }
            catch (InvalidDataException)
            {
                return 1;
            }
        });

        int exitCode = await root.Parse(new[] { "state", "restore", corruptPath }).InvokeAsync();

        Assert.Equal(1, exitCode);
        _mockStateService.Verify(s => s.RestoreState(corruptPath), Times.Once);
    }

    [Fact]
    public async Task StateClear_InvokesClearAction()
    {
        // Arrange
        string? capturedAction = null;
        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            capturedAction = action;
            return 0;
        });

        // Backup the original standard input stream
        var originalIn = Console.In;

        try
        {
            // Simulate user pressing 'y'
            Console.SetIn(new StringReader("y"));

            // Act
            int exitCode = await root.Parse(new[] { "state", "clear" }).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Equal("clear", capturedAction);
        }
        finally
        {
            // Always safely restore standard input, even if assertions or execution fail
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public async Task StateList_WithVerboseFlag_PassesTraceLogLevel()
    {
        LogLevel? capturedLevel = null;
        _mockStateService.Setup(s => s.ListItems()).Returns(new HashSet<string> { "item1" });

        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            capturedLevel = logLevel;
            _mockStateService.Object.ListItems();
            return 0;
        });

        await root.Parse(new[] { "state", "list", "--verbose" }).InvokeAsync();

        Assert.Equal(LogLevel.Trace, capturedLevel);
    }

    [Fact]
    public async Task StateList_WithQuietFlag_PassesWarningLogLevel()
    {
        LogLevel? capturedLevel = null;
        _mockStateService.Setup(s => s.ListItems()).Returns(new HashSet<string> { "item1" });

        var root = BuildRealCommand(async (action, path, logLevel) =>
        {
            capturedLevel = logLevel;
            _mockStateService.Object.ListItems();
            return 0;
        });

        await root.Parse(new[] { "state", "list", "--quiet" }).InvokeAsync();

        Assert.Equal(LogLevel.Warning, capturedLevel);
    }

    [Fact]
    public async Task StateList_VerboseAndQuiet_ReturnsConflictError()
    {
        var root = BuildRealCommand(async (action, path, logLevel) => 0);

        int exitCode = await root.Parse(new[] { "state", "list", "--verbose", "--quiet" }).InvokeAsync();

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task State_UnknownSubcommand_ReturnsNonZeroExit()
    {
        var root = BuildRealCommand(async (action, path, logLevel) => 0);

        int exitCode = await root.Parse(new[] { "state", "unknownsubcmd" }).InvokeAsync();

        Assert.NotEqual(0, exitCode);
    }
}

/// <summary>
/// A bulletproof, disposable class to redirect and capture Console output.
/// Guarantees that Console.Out is returned to normal even if a test explodes.
/// </summary>
public class ConsoleOutputInterceptor : IDisposable
{
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOutput;

    public ConsoleOutputInterceptor()
    {
        _stringWriter = new StringWriter();
        _originalOutput = Console.Out;
        Console.SetOut(_stringWriter);
    }

    public string GetOutput() => _stringWriter.ToString();

    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        _stringWriter.Dispose();
    }
}