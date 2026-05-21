using WinHome.Models;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for interactively generating a WinHome configuration.
    /// </summary>
    public interface IGeneratorService
    {
        /// <summary>
        /// Asynchronously generates a new <see cref="Configuration"/> by prompting the user.
        /// </summary>
        /// <returns>A task that resolves to the generated <see cref="Configuration"/>.</returns>
        Task<Configuration> GenerateAsync();
    }
}