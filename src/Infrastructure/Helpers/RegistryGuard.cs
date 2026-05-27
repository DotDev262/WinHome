using System.Security.Principal;
using System.Runtime.Versioning;

namespace WinHome.Infrastructure.Helpers
{
    [SupportedOSPlatform("windows")]
    public static class RegistryGuard
    {
        internal static Func<bool> IsSystemUser = () => WindowsIdentity.GetCurrent().IsSystem;

        public static void ValidateContext(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath)) return;

            bool isUserHive = keyPath.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase) ||
                              keyPath.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase);

            if (isUserHive && IsSystemUser())
            {
                throw new InvalidOperationException(
                    "Security Risk: Attempting to modify HKCU while running as SYSTEM. " +
                    "This will apply settings to the LocalSystem profile (S-1-5-18), not the logged-in user. " +
                    "Please use the full HKEY_USERS\\<SID> path instead to target a specific user.");
            }
        }

        internal static void ResetSystemUserCheck()
        {
            IsSystemUser = () => WindowsIdentity.GetCurrent().IsSystem;
        }
    }
}
