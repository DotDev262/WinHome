namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for resolving secret placeholder values in configuration strings.
    /// </summary>
    public interface ISecretResolver
    {
        /// <summary>
        /// Resolves any secret placeholders in the given input string.
        /// </summary>
        /// <param name="input">The string potentially containing secret placeholders.</param>
        /// <returns>The string with all placeholders replaced by their resolved values.</returns>
        string Resolve(string input);

        /// <summary>
        /// Recursively resolves secret placeholders on all string properties of the given object.
        /// </summary>
        /// <param name="obj">The object whose string properties should be resolved.</param>
        void ResolveObject(object obj);
    }
}