namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for checking and applying application updates.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Checks whether a newer version of the application is available.
        /// </summary>
        /// <param name="currentVersion">The currently installed version string.</param>
        /// <returns>A task resolving to <c>true</c> if an update is available; otherwise <c>false</c>.</returns>
        Task<bool> CheckForUpdatesAsync(string currentVersion);

        /// <summary>
        /// Downloads and applies the latest available update.
        /// </summary>
        /// <returns>A task representing the asynchronous update operation.</returns>
        Task UpdateAsync();
    }
}