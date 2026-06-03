using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Generates a default <see cref="Configuration"/> by scanning the current system state.</summary>
  public interface IGeneratorService
  {
    /// <summary>Scans the current system and returns a generated <see cref="Configuration"/>.</summary>
    /// <returns>A <see cref="Configuration"/> representing current system state.</returns>
    Task<Configuration> GenerateAsync();
  }
}
