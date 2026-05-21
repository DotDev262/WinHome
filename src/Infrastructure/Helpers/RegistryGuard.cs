using System.Security.Principal;
using System.Runtime.Versioning;

namespace WinHome.Infrastructure.Helpers
{
    /// <summary>
    /// Provides security assertion guards and context validation checks for Windows Registry operations 
    /// to prevent accidental state corruption during elevated execution lifecycles.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class RegistryGuard
    {
        /// <summary>
        /// Validates the current process's execution identity security context against the target registry key hive path 
        /// to ensure user-hive operations do not inadvertently target the LocalSystem profile.
        /// </summary>
        /// <param name="keyPath">The target registry key absolute path string being evaluated for modification.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to alter an HKCU path while executing under the NT AUTHORITY\SYSTEM identity context.
        /// </exception>
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