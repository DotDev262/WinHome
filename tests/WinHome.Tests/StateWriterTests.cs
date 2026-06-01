using System;
using System.IO;
using System.Text.Json;
using WinHome.Services;
using Xunit;

namespace WinHome.Tests
{
  public class StateWriterTests
  {
    [Fact]
    public void Load_CorruptedFile_ShouldNotThrowAndReturnEmpty()
    {
      var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
      try
      {
        File.WriteAllText(tmp, "{ invalid json }");

        var writer = new StateWriter(tmp);
        var loaded = writer.Load();

        Assert.NotNull(loaded);
        Assert.Empty(loaded);
      }
      finally
      {
        if (File.Exists(tmp)) File.Delete(tmp);
      }
    }

    [Fact]
    public void RecordStep_ShouldWriteAndLoad()
    {
      var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
      try
      {
        var writer = new StateWriter(tmp);

        var step = new WinHome.Models.StepResult
        {
          StepId = "winget:TestApp",
          StepType = "app",
          StepName = "TestApp",
          Status = WinHome.Models.StepStatus.Succeeded,
          AppliedAt = DateTime.UtcNow
        };

        writer.RecordStep(step);
        var loaded = writer.Load();

        Assert.True(loaded.ContainsKey(step.StepId));
        Assert.Equal(WinHome.Models.StepStatus.Succeeded, loaded[step.StepId].Status);
      }
      finally
      {
        if (File.Exists(tmp)) File.Delete(tmp);
        if (File.Exists(tmp + ".tmp")) File.Delete(tmp + ".tmp");
      }
    }
  }
}
