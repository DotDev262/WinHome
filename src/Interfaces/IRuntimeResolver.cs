namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for resolving the executable path of a named runtime.
    /// </summary>
    public interface IRuntimeResolver
    {
        /// <summary>
        /// Resolves the path or command for the specified runtime name.
        /// </summary>
        /// <param name="runtimeName">The name of the runtime to resolve (e.g., "node", "python").</param>
        /// <returns>The resolved path or command string for the runtime.</returns>
        string Resolve(string runtimeName);
    }
}