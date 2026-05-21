using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for managing system environment variables.
    /// </summary>
    public interface IEnvironmentService
    {
        /// <summary>
        /// Applies the specified environment variable configuration to the system.
        /// </summary>
        /// <param name="env">The environment variable configuration to apply.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Apply(EnvVarConfig env, bool dryRun);

        /// <summary>
        /// Refreshes the system PATH environment variable.
        /// </summary>
        void RefreshPath();
    }
}