using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WinHome.Models;
using WinHome.Interfaces;
using System.CommandLine;

namespace WinHome.Infrastructure
{
  /// <summary>Registers the `status` subcommand that displays the apply state from the unified state file.</summary>
  public static class StatusCommand
  {
    /// <summary>Registers the status command onto the root command.</summary>
    /// <param name="root">The root command to attach to.</param>
    /// <param name="services">Service provider for resolving dependencies.</param>
    public static void Register(RootCommand root, IServiceProvider services)
    {
      var cmd = new Command("status") { Description = "Show apply status from unified state" };

      cmd.SetAction((ParseResult result) =>
      {
        var logger = services.GetService<ILogger>();
        var stateService = services.GetService<IStateService>();
        if (stateService == null)
        {
          logger?.LogError("State service not available.");
          return 1;
        }

        var state = stateService.ListSteps();
        if (!state.Any())
        {
          Console.WriteLine("No apply state found.");
          return 0;
        }

        int succeeded = 0, failed = 0, skipped = 0;
        foreach (var kv in state.OrderBy(k => k.Key))
        {
          var r = kv.Value;
          var icon = r.Status switch
          {
            StepStatus.Succeeded => "[OK]",
            StepStatus.Failed => "[FAIL]",
            StepStatus.Skipped => "[SKIP]",
            _ => "[?]"
          };

          string label = r.StepName ?? kv.Key;
          if (!string.IsNullOrEmpty(r.StepType)) label = $"{r.StepType}.{label}";

          Console.WriteLine($"{icon} {label}");

          if (r.Status == StepStatus.Succeeded) succeeded++;
          else if (r.Status == StepStatus.Failed) failed++;
          else if (r.Status == StepStatus.Skipped) skipped++;
        }

        Console.WriteLine();
        Console.WriteLine($"{succeeded} succeeded · {failed} failed · {skipped} skipped");
        return 0;
      });

      root.Add(cmd);
    }
  }
}
