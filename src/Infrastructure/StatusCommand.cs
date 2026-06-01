using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WinHome.Services;
using WinHome.Models;
using WinHome.Interfaces;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace WinHome.Infrastructure
{
  public static class StatusCommand
  {
    public static void Register(RootCommand root, IServiceProvider services)
    {
      var cmd = new Command("status") { Description = "Show apply status from .winhome-state.json" };

      cmd.SetAction((ParseResult result) =>
      {
        var logger = services.GetService<ILogger>();
        var writer = services.GetService<StateWriter>();
        if (writer == null)
        {
          logger?.LogError("State writer not available.");
          return 1;
        }

        var state = writer.Load();
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

