using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for applying Windows scheduled task configurations.
    /// </summary>
    public interface IScheduledTaskService
    {
        /// <summary>
        /// Applies the specified scheduled task configuration to the system.
        /// </summary>
        /// <param name="task">The scheduled task configuration to apply.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Apply(ScheduledTaskConfig task, bool dryRun);
    }
}