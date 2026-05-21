using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for configuring Windows Subsystem for Linux (WSL).
    /// </summary>
    public interface IWslService
    {
        /// <summary>
        /// Applies the specified WSL configuration to the system.
        /// </summary>
        /// <param name="config">The WSL configuration to apply.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Configure(WslConfig config, bool dryRun);
    }
}