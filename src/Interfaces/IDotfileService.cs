using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for applying dotfile configurations to the system.
    /// </summary>
    public interface IDotfileService
    {
        /// <summary>
        /// Applies the specified dotfile configuration to the system.
        /// </summary>
        /// <param name="dotfile">The dotfile configuration to apply.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        void Apply(DotfileConfig dotfile, bool dryRun);
    }
}