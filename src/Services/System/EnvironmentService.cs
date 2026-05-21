using System.Runtime.Versioning;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Provides concrete operations to query, mutate, append, and refresh Windows environment variables 
    /// explicitly bounded within specific user and session execution scopes.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class EnvironmentService : IEnvironmentService
    {
        private readonly ILogger _logger;
        
        /// <summary>
        /// Specifies the targeted scope for environment changes. Strictly isolates targets to the User hive 
        /// to ensure execution safety without requiring elevated administrative privileges.
        /// </summary>
        private const EnvironmentVariableTarget Target = EnvironmentVariableTarget.User;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentService"/> class with operational logger dependencies.
        /// </summary>
        /// <param name="logger">The diagnostic log tracker capture utility routing operations and errors.</param>
        public EnvironmentService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Evaluates and applies structural state changes to a targeted environment variable based on configuration directives.
        /// </summary>
        /// <param name="env">The configuration data context specifying the targeted variable name, mutation payload value, and operation type.</param>
        /// <param name="dryRun">A conditional execution safety modifier flag which, when <c>true</c>, exits early and avoids mutating system registry states.</param>
        public void Apply(EnvVarConfig env, bool dryRun)
        {
            if (string.IsNullOrEmpty(env.Variable)) return;

            string currentValue = Environment.GetEnvironmentVariable(env.Variable, Target) ?? string.Empty;
            string newValue = env.Value;

            // Handle Path Appending
            if (env.Action.Equals("append", StringComparison.OrdinalIgnoreCase))
            {
                var parts = currentValue.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();

                // Idempotency check: Skip configuration if the value is already present in the collection
                if (parts.Contains(newValue, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogSuccess($"[Env] Skipped: '{newValue}' already in {env.Variable}");
                    return;
                }

                newValue = string.IsNullOrEmpty(currentValue) ? newValue : $"{currentValue};{newValue}";
            }
            else
            {
                // Handle Set (Overwrite verification)
                if (currentValue == newValue)
                {
                    _logger.LogSuccess($"[Env] Skipped: {env.Variable} is already correct.");
                    return;
                }
            }

            if (dryRun)
            {
                _logger.LogError($"[DryRun] Would set User Env Var '{env.Variable}' to '{newValue}'");
                return;
            }

            try
            {
                Environment.SetEnvironmentVariable(env.Variable, newValue, Target);
                _logger.LogSuccess($"[Env] Set User variable {env.Variable} = {env.Value}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] Failed to set Env Var: {ex.Message}");
            }
        }

        /// <summary>
        /// Re-reads and synchronizes the active runtime process execution PATH string collection by aggregating 
        /// and distinct-filtering local Machine and user registry configuration settings.
        /// </summary>
        public void RefreshPath()
        {
            try
            {
                // Reload Machine PATH
                string machinePath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine) ?? string.Empty;
                // Reload User PATH
                string userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? string.Empty;

                // Merge collections with an identity distinct-filter mapping
                var combinedPath = string.Join(";",
                    machinePath.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Concat(userPath.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

                // Update CURRENT process environment block mapping
                Environment.SetEnvironmentVariable("Path", combinedPath, EnvironmentVariableTarget.Process);
                _logger.LogSuccess("[Env] Refreshed process PATH from registry.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Env] Failed to refresh process PATH: {ex.Message}");
            }
        }
    }
}