using System.Text.Json;
using Moq;
using WinHome.Interfaces;
using WinHome.Services.System;
using WinHome.Models;
using Xunit;

namespace WinHome.Tests.Services.System
{
  [Collection("StateService")]
  public class StateServiceTests : IDisposable
  {
    private readonly string _testDir;
    private readonly string _stateFilePath;
    private readonly Mock<ILogger> _mockLogger;

    private readonly string? _originalEnvPath;

    public StateServiceTests()
    {
      _originalEnvPath = Environment.GetEnvironmentVariable("WINHOME_STATE_PATH");
      _testDir = Path.Combine(Path.GetTempPath(), $"WinHomeStateTests_{Guid.NewGuid()}");
      Directory.CreateDirectory(_testDir);
      _stateFilePath = Path.Combine(_testDir, "winhome.state.json");
      _mockLogger = new Mock<ILogger>();
    }

    public void Dispose()
    {
      Environment.SetEnvironmentVariable("WINHOME_STATE_PATH", _originalEnvPath);
      if (Directory.Exists(_testDir))
        Directory.Delete(_testDir, recursive: true);
    }

    /// <summary>Creates a StateService pointing at the test-directory state file.</summary>
    private StateService CreateService()
    {
      Environment.SetEnvironmentVariable("WINHOME_STATE_PATH", _stateFilePath);
      return new StateService(_mockLogger.Object);
    }

    // ── Valid state ────────────────────────────────────────────────────────────

    [Fact]
    public void LoadState_LegacyJson_ReturnsExpectedItems()
    {
      var expected = new HashSet<string> { "packageA", "packageB" };
      File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(expected));

      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Equal(expected, state.AppliedItems);
      _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoadState_StateDataJson_ReturnsExpectedItems()
    {
      var expectedState = new StateData
      {
        AppliedItems = new HashSet<string> { "pkg1" },
        SystemSettingOriginals = new Dictionary<string, object> { { "setting1", "val1" } }
      };
      File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(expectedState));

      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Equal(expectedState.AppliedItems, state.AppliedItems);
      Assert.Equal("val1", state.SystemSettingOriginals["setting1"]?.ToString());
      _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoadState_MissingFile_ReturnsEmptyState()
    {
      // Don't create the file at all
      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Empty(state.AppliedItems);
      _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
    }

    // ── Corrupted JSON ─────────────────────────────────────────────────────────

    [Fact]
    public void LoadState_CorruptedJson_ReturnsEmptyState()
    {
      File.WriteAllText(_stateFilePath, "{this is not valid json");

      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Empty(state.AppliedItems);
    }

    [Fact]
    public void LoadState_TruncatedJson_ReturnsEmptyState()
    {
      // Simulates a partial write: the array was started but never finished
      File.WriteAllText(_stateFilePath, "[\"packageA\",");

      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Empty(state.AppliedItems);
    }

    [Fact]
    public void LoadState_EmptyFile_ReturnsEmptyState()
    {
      File.WriteAllText(_stateFilePath, string.Empty);

      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Empty(state.AppliedItems);
    }

    [Fact]
    public void LoadState_WrongJsonType_ReturnsEmptyState()
    {
      // A valid JSON primitive instead of an object/array — wrong type, will fail both format deserializations and trigger corruption backup
      File.WriteAllText(_stateFilePath, "\"a string, not an object\"");

      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Empty(state.AppliedItems);
    }

    // ── Corruption backup ──────────────────────────────────────────────────────

    [Fact]
    public void LoadState_CorruptedJson_CreatesBackupFile()
    {
      File.WriteAllText(_stateFilePath, "CORRUPTED DATA !!!!");

      var svc = CreateService();
      svc.LoadState();

      // The original file should have been renamed to a .corrupted.<timestamp> file
      var backups = Directory.GetFiles(_testDir, "winhome.state.json.corrupted.*");
      Assert.Single(backups);
    }

    [Fact]
    public void LoadState_BackupFails_LogsWarningAndReturnsEmpty()
    {
      if (!OperatingSystem.IsWindows()) return;

      File.WriteAllText(_stateFilePath, "{ invalid json");

      // Lock the file to force File.Move to throw an exception
      using var lockStream = new FileStream(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Empty(state.AppliedItems);
      _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Could not back up corrupted state file"))), Times.AtLeastOnce);
    }

    [Fact]
    public void LoadState_CorruptedJson_LogsWarningWithPath()
    {
      File.WriteAllText(_stateFilePath, "{{BAD}}");

      var svc = CreateService();
      svc.LoadState();

      _mockLogger.Verify(l =>
          l.LogWarning(It.Is<string>(msg =>
              msg.Contains("[State]") &&
              msg.Contains("corrupted") &&
              msg.Contains("winhome.state.json"))),
          Times.AtLeastOnce);
    }

    [Fact]
    public void LoadState_CorruptedJson_OriginalFileRemovedAfterBackup()
    {
      File.WriteAllText(_stateFilePath, "CORRUPTED DATA !!!!");

      var svc = CreateService();
      svc.LoadState();

      // The original file should no longer exist (it was moved to .corrupted.*)
      Assert.False(File.Exists(_stateFilePath));
    }

    // ── Save & MarkAsApplied ───────────────────────────────────────────────────

    [Fact]
    public void MarkAsApplied_PersistsItemToDisk()
    {
      var svc = CreateService();
      svc.MarkAsApplied("myPackage");

      Assert.True(File.Exists(_stateFilePath));
      var written = JsonSerializer.Deserialize<StateData>(File.ReadAllText(_stateFilePath));
      Assert.Contains("myPackage", written?.AppliedItems!);
    }

    [Fact]
    public void RemoveApplied_RemovesItemFromDisk()
    {
      var svc = CreateService();
      svc.MarkAsApplied("oldPackage");

      svc.RemoveApplied("oldPackage");

      var written = JsonSerializer.Deserialize<StateData>(File.ReadAllText(_stateFilePath));
      Assert.DoesNotContain("oldPackage", written?.AppliedItems!);
    }

    [Fact]
    public void SaveState_WritesAllItemsToDisk()
    {
      var items = new HashSet<string> { "a", "b", "c" };
      var svc = CreateService();
      svc.SaveState(new StateData { AppliedItems = items });

      var written = JsonSerializer.Deserialize<StateData>(File.ReadAllText(_stateFilePath));
      Assert.Equal(items, written?.AppliedItems);
    }

    // ── Partial-write / incomplete JSON ───────────────────────────────────────

    [Fact]
    public void LoadState_PartialWrite_TreatedAsCorruption_AndBackupCreated()
    {
      // Simulate a partial write (e.g. process crashed mid-flush)
      File.WriteAllText(_stateFilePath, "[\"item1\", \"item2\", \"item3");

      var svc = CreateService();
      var state = svc.LoadState();

      Assert.Empty(state.AppliedItems);
      var backups = Directory.GetFiles(_testDir, "winhome.state.json.corrupted.*");
      Assert.Single(backups);
    }

    // ── Round-trip: recover then continue working ─────────────────────────────

    [Fact]
    public void AfterCorruption_CanSaveAndLoadNewState()
    {
      File.WriteAllText(_stateFilePath, "NOT JSON AT ALL");

      var svc = CreateService();
      svc.LoadState(); // triggers recovery

      // Now add an item — should write a fresh valid file
      svc.MarkAsApplied("newItem");

      // Re-create the service to force a fresh load from disk
      var svc2 = CreateService();
      var state = svc2.LoadState();

      Assert.Contains("newItem", state.AppliedItems);
    }
  }
}
