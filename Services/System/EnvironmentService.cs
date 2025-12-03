using System.Runtime.Versioning;
using WinHome.Models;

namespace WinHome.Services.System
{
    [SupportedOSPlatform("windows")]
    public class EnvironmentService
    {
        // We strictly target the USER scope. No Admin needed.
        private const EnvironmentVariableTarget Target = EnvironmentVariableTarget.User;

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
                    Console.WriteLine($"[Env] Skipped: '{newValue}' already in {env.Variable}");
                    return;
                }

                newValue = string.IsNullOrEmpty(currentValue) ? newValue : $"{currentValue};{newValue}";
            }
            else
            {
                // Handle Set (Overwrite)
                if (currentValue == newValue)
                {
                    Console.WriteLine($"[Env] Skipped: {env.Variable} is already correct.");
                    return;
                }
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would set User Env Var '{env.Variable}' to '{newValue}'");
                Console.ResetColor();
                return;
            }

            try 
            {
                Environment.SetEnvironmentVariable(env.Variable, newValue, Target);
                Console.WriteLine($"[Env] Set User variable {env.Variable} = {env.Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to set Env Var: {ex.Message}");
            }
        }
    }
}