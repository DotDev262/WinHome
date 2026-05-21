using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for managing Windows service configurations.
    /// </summary>
    public interface IWindowsServiceManager
    {
        /// <summary>
        /// Applies the specified Windows service configuration to the system.
        /// </summary>
        /// <param name="service">The Windows service configuration to apply.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Apply(WindowsServiceConfig service, bool dryRun);
    }
}