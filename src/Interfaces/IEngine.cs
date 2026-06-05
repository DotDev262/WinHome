using System.Threading.Tasks;
using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Main orchestrator for WinHome configuration application.</summary>
  public interface IEngine
  {
    Task RunAsync(Configuration config, bool dryRun, string? profileName = null, bool debug = false, bool diff = false, bool forceReapply = false, bool continueOnError = false);
  }
}
