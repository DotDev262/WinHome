namespace WinHome.Interfaces
{
  /// <summary>Resolves secret references (e.g. environment variables, Windows Credential Manager) in configuration values.</summary>
  public interface ISecretResolver
  {
    /// <summary>Resolves secret references in the given input string.</summary>
    /// <returns>The resolved string with secrets replaced.</returns>
    string Resolve(string input);
    /// <summary>Recursively resolves secret references in all string properties of an object.</summary>
    void ResolveObject(object obj);
  }
}
