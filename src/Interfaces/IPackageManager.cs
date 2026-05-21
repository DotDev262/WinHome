using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for a package manager that can install and manage applications.
    /// </summary>
    public interface IPackageManager
    {
        /// <summary>
        /// Gets the bootstrapper responsible for installing this package manager.
        /// </summary>
        IPackageManagerBootstrapper Bootstrapper { get; }

        /// <summary>
        /// Determines whether this package manager is available on the current system.
        /// </summary>
        /// <returns><c>true</c> if available; otherwise <c>false</c>.</returns>
        bool IsAvailable();

        /// <summary>
        /// Installs the specified application.
        /// </summary>
        /// <param name="app">The application configuration to install.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Install(AppConfig app, bool dryRun);

        /// <summary>
        /// Uninstalls the application with the specified ID.
        /// </summary>
        /// <param name="appId">The unique identifier of the application to uninstall.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Uninstall(string appId, bool dryRun);

        /// <summary>
        /// Checks whether the application with the specified ID is currently installed.
        /// </summary>
        /// <param name="appId">The unique identifier of the application to check.</param>
        /// <returns><c>true</c> if installed; otherwise <c>false</c>.</returns>
        bool IsInstalled(string appId);
    }
}