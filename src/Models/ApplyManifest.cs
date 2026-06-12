using System;

namespace WinHome.Models
{
  /// <summary>Represents the outcome of a single apply step.</summary>
  public enum StepStatus
  {
    /// <summary>The step completed successfully.</summary>
    Succeeded,
    /// <summary>The step encountered an error.</summary>
    Failed,
    /// <summary>The step was skipped (e.g. dry run or condition not met).</summary>
    Skipped
  }

  /// <summary>Records the result of applying a single configuration step.</summary>
  public record StepResult
  {
    public string StepId { get; init; } = string.Empty;
    public string? StepType { get; init; }
    public string? StepName { get; init; }
    public StepStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? AppliedAt { get; init; }
  }
}
