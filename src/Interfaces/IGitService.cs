using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for applying Git configuration settings to the system.
    /// </summary>
    public interface IGitService
    {
        /// <summary>
        /// Applies the specified Git configuration to the system.
        /// </summary>
        /// <param name="config">The Git configuration settings to apply.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Configure(GitConfig config, bool dryRun);
    }
}