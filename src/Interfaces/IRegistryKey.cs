using Microsoft.Win32;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for interacting with a Windows registry key.
    /// </summary>
    public interface IRegistryKey : IDisposable
    {
        /// <summary>
        /// Sets a named value in this registry key.
        /// </summary>
        /// <param name="name">The name of the value to set.</param>
        /// <param name="value">The data to store.</param>
        /// <param name="kind">The registry data type.</param>
        void SetValue(string name, object value, RegistryValueKind kind);

        /// <summary>
        /// Gets the data associated with a named value.
        /// </summary>
        /// <param name="name">The name of the value to retrieve.</param>
        /// <returns>The value data, or <c>null</c> if not found.</returns>
        object? GetValue(string name);

        /// <summary>
        /// Deletes the named value from this registry key.
        /// </summary>
        /// <param name="name">The name of the value to delete.</param>
        void DeleteValue(string name);

        /// <summary>
        /// Opens a named subkey of this registry key.
        /// </summary>
        /// <param name="name">The name of the subkey to open.</param>
        /// <param name="writable">If <c>true</c>, opens the subkey with write access.</param>
        /// <returns>The opened <see cref="IRegistryKey"/>, or <c>null</c> if not found.</returns>
        IRegistryKey? OpenSubKey(string name, bool writable);

        /// <summary>
        /// Creates or opens a named subkey of this registry key.
        /// </summary>
        /// <param name="name">The name of the subkey to create or open.</param>
        /// <param name="writable">If <c>true</c>, opens the subkey with write access.</param>
        /// <returns>The created or opened <see cref="IRegistryKey"/>.</returns>
        IRegistryKey? CreateSubKey(string name, bool writable);
    }
}