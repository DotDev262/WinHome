using System.Runtime.Versioning;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    [SupportedOSPlatform("windows")]
    public class EnvironmentService : IEnvironmentService
    {
        private readonly ILogger _logger;
        // We strictly target the USER scope. No Admin needed.
        private const EnvironmentVariableTarget Target = EnvironmentVariableTarget.User;

        public EnvironmentService(ILogger logger)
        {
            _logger = logger;
        }

        public void Apply(EnvVarConfig env, bool dryRun)
        {
            if (string.IsNullOrEmpty(env.Variable)) return;

            string currentValue = Environment.GetEnvironmentVariable(env.Variable, Target) ?? string.Empty;
            string newValue = env.Value;

            // Handle Path Appending
            if (env.Action.ToLower() == "append")
            {
                var parts = currentValue.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();

                // Idempotency: Don't add if already there
                if (parts.Contains(newValue, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogInfo($"[Env] Skipped: '{newValue}' already in {env.Variable}");
                    return;
                }

                newValue = string.IsNullOrEmpty(currentValue) ? newValue : $"{currentValue};{newValue}";
            }
            else
            {
                // Handle Set (Overwrite)
                if (currentValue == newValue)
                {
                    _logger.LogInfo($"[Env] Skipped: {env.Variable} is already correct.");
                    return;
                }
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would set User Env Var '{env.Variable}' to '{newValue}'");
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

        public void RefreshPath()
        {
            try
            {
                // Reload Machine PATH
                string machinePath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine) ?? string.Empty;
                // Reload User PATH
                string userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? string.Empty;

                // Merge them
                var combinedPath = string.Join(";", 
                    machinePath.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Concat(userPath.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

                // Update CURRENT process environment
                Environment.SetEnvironmentVariable("Path", combinedPath, EnvironmentVariableTarget.Process);
                _logger.LogInfo("[Env] Refreshed process PATH from registry.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Env] Failed to refresh process PATH: {ex.Message}");
            }
        }
    }
}