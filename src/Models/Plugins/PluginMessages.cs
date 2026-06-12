using System.Text.Json.Serialization;

namespace WinHome.Models.Plugins
{
  /// <summary>Message sent from WinHome to a plugin process requesting command execution.</summary>
  public class PluginRequest
  {
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public object? Args { get; set; }

    [JsonPropertyName("context")]
    public object? Context { get; set; }
  }

  /// <summary>Response from a plugin process after executing a command.</summary>
  public class PluginResult
  {
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    private bool? _success;

    [JsonPropertyName("success")]
    public bool Success
    {
      get => _success ?? string.IsNullOrEmpty(Error);
      set => _success = value;
    }

    [JsonPropertyName("changed")]
    public bool Changed { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("installed")]
    public bool? Installed { get; set; }
  }
}
