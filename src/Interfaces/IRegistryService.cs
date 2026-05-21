using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for applying and managing Windows registry tweaks.
    /// </summary>
    public interface IRegistryService
    {
        /// <summary>
        /// Applies the specified registry tweak to the system.
        /// </summary>
        /// <param name="tweak">The registry tweak to apply.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Apply(RegistryTweak tweak, bool dryRun);

        /// <summary>
        /// Reverts a registry value at the specified path and name.
        /// </summary>
        /// <param name="path">The full registry path of the key.</param>
        /// <param name="name">The name of the value to revert.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Revert(string path, string name, bool dryRun);

        /// <summary>
        /// Reads the current value of a registry entry.
        /// </summary>
        /// <param name="path">The full registry path of the key.</param>
        /// <param name="name">The name of the value to read.</param>
        /// <returns>The current registry value, or <c>null</c> if not found.</returns>
        object? Read(string path, string name);
    }
}