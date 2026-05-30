using System;

namespace WinHome.Models
{
  public enum StepStatus
  {
    Succeeded,
    Failed,
    Skipped
  }

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

