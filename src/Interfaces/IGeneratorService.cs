using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IGeneratorService
    {
        Task<Configuration> GenerateAsync();
    }
}
