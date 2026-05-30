namespace WinHome.Interfaces
{
  /// <summary>Service for checking and applying WinHome self-updates.</summary>
  public interface IUpdateService
  {
    /// <summary>Checks whether a newer version is available.</summary>
    /// <param name="currentVersion">The currently installed version string.</param>
    /// <returns><c>true</c> if an update is available.</returns>
    Task<bool> CheckForUpdatesAsync(string currentVersion);
    /// <summary>Downloads and applies the latest update.</summary>
    Task UpdateAsync();
  }
}
