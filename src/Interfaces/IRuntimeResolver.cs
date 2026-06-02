namespace WinHome.Interfaces
{
  /// <summary>Resolves runtime executables (e.g. node, python, dotnet) by name.</summary>
  public interface IRuntimeResolver
  {
    /// <summary>Resolves the full path to the given runtime executable.</summary>
    /// <param name="runtimeName">Runtime name (e.g. "node", "python", "dotnet").</param>
    /// <returns>Full path to the runtime executable.</returns>
    string Resolve(string runtimeName);
  }
}
