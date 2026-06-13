using Microsoft.Extensions.Logging;
using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;
using Microsoft.Win32.TaskScheduler;
using System;

namespace WinHome.Tests
{
  public class ScheduledTaskServiceTests
  {
    private readonly Mock<ILogger<ScheduledTaskService>> _mockLogger;
    private readonly ScheduledTaskService _service;

    public ScheduledTaskServiceTests()
    {
      _mockLogger = new Mock<ILogger<ScheduledTaskService>>();
      _service = new ScheduledTaskService(_mockLogger.Object);
    }

    [Fact]
    public void Apply_ShouldCreateScheduledTask()
    {
      if (!OperatingSystem.IsWindows()) return;

      // Arrange
      var taskConfig = new ScheduledTaskConfig
      {
        Name = "TestTask",
        Path = "",
        Description = "A test task",
        Author = "Test Author",
        Triggers = new()
                {
                    new TriggerConfig
                    {
                        Type = "Daily"
                    }
                },
        Actions = new()
                {
                    new ActionConfig
                    {
                        Type = "Exec",
                        Path = "cmd.exe",
                        Arguments = "/c echo Test"
                    }
                }
      };

      // Act
      _service.Apply(taskConfig, false);

      // Assert
      using (var ts = new TaskService())
      {
        var task = ts.FindTask(taskConfig.Name);
        Assert.NotNull(task);
        Assert.Equal("A test task", task.Definition.RegistrationInfo.Description);
        Assert.Equal("Test Author", task.Definition.RegistrationInfo.Author);
        ts.RootFolder.DeleteTask(taskConfig.Name);
      }
    }

    [Fact]
    public void Apply_DryRun_ShouldNotCreateScheduledTask()
    {
      if (!OperatingSystem.IsWindows()) return;

      // Arrange
      var taskConfig = new ScheduledTaskConfig
      {
        Name = "TestTask",
        Path = "",
      };

      // Act
      _service.Apply(taskConfig, true);

      // Assert
      using (var ts = new TaskService())
      {
        var task = ts.FindTask(taskConfig.Name);
        Assert.Null(task);
      }
    }
  }
}
