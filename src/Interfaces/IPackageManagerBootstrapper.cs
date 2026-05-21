namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for bootstrapping (self-installing) a package manager.
    /// </summary>
    public interface IPackageManagerBootstrapper
    {
        /// <summary>
        /// Gets the display name of this package manager.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determines whether this package manager is already installed on the system.
        /// </summary>
        /// <returns><c>true</c> if installed; otherwise <c>false</c>.</returns>
        bool IsInstalled();

        /// <summary>
        /// Installs this package manager on the system.
        /// </summary>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Install(bool dryRun);
    }
}