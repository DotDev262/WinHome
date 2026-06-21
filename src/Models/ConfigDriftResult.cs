namespace WinHome.Models;

public class ConfigDriftResult
{
  public string Provider { get; set; } = "";

  public string Key { get; set; } = "";

  public string? Expected { get; set; }

  public string? Actual { get; set; }
}
