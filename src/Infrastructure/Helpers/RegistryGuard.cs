using System.Security.Principal;
using System.Runtime.Versioning;

namespace WinHome.Infrastructure.Helpers
{
    [SupportedOSPlatform("windows")]
    public static class RegistryGuard
    {
        public static void ValidateContext(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath)) return;

            // Normalize check for HKCU
            bool isUserHive = keyPath.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase) || 
                              keyPath.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase);

            if (isUserHive)
            {
                var currentIdentity = WindowsIdentity.GetCurrent();
                if (currentIdentity.IsSystem)
                {
                    throw new InvalidOperationException(
                        "Security Risk: Attempting to modify HKCU while running as SYSTEM. " +
                        "This will apply settings to the LocalSystem profile (S-1-5-18), not the logged-in user. " +
                        "Please use the full HKEY_USERS\\<SID> path instead to target a specific user.");
                }
            }
        }
    }
}
