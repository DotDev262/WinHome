using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Service for creating and managing Windows Scheduled Tasks.</summary>
  public interface IScheduledTaskService
  {
    /// <summary>Creates or updates a scheduled task from the given configuration.</summary>
    /// <param name="task">Scheduled task configuration.</param>
    /// <param name="dryRun">If <c>true</c>, simulates without making changes.</param>
    void Apply(ScheduledTaskConfig task, bool dryRun);
  }
}
