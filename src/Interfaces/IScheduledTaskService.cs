using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IScheduledTaskService
    {
        void Apply(ScheduledTaskConfig task, bool dryRun);
    }
}
