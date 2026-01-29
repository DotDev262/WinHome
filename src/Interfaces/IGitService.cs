using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IGitService
    {
        void Configure(GitConfig config, bool dryRun);
    }
}
