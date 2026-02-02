namespace WinHome.Interfaces
{
    public interface IUpdateService
    {
        Task<bool> CheckForUpdatesAsync(string currentVersion);
        Task UpdateAsync();
    }
}
