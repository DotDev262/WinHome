using System.Security.Principal;
using System.Runtime.Versioning;

namespace WinHome.Infrastructure.Helpers;

/// <summary>Validates that the current process has administrative privileges.</summary>
[SupportedOSPlatform("windows")]
public static class AdminGuard
{
  internal static Func<bool> IsAdministrator = () =>
  {
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
  };

  /// <summary>Throws if the current process is not running as Administrator.</summary>
  /// <exception cref="UnauthorizedAccessException">Thrown when not running with admin privileges.</exception>
  public static void EnsureAdministrator()
  {
    if (!IsAdministrator())
    {
      throw new UnauthorizedAccessException(
        "Error: WinHome requires Administrative Privileges to manage system configurations. " +
        "Please re-run this command from an elevated (Administrator) terminal.");
    }
  }

  /// <summary>Resets the administrator check delegate to its default implementation (used for testing).</summary>
  internal static void ResetAdminCheck()
  {
    IsAdministrator = () =>
    {
      using var identity = WindowsIdentity.GetCurrent();
      var principal = new WindowsPrincipal(identity);
      return principal.IsInRole(WindowsBuiltInRole.Administrator);
    };
  }
}