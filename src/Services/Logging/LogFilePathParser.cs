namespace WinHome.Services.Logging;

public static class LogFilePathParser
{
  public static string? Parse(string[] args)
  {
    for (var i = 0; i < args.Length; i++)
    {
      if (args[i] == "--log-file" && i + 1 < args.Length)
      {
        return args[i + 1];
      }

      const string prefix = "--log-file=";
      if (args[i].StartsWith(prefix, StringComparison.Ordinal))
      {
        return args[i][prefix.Length..];
      }
    }

    return null;
  }
}
