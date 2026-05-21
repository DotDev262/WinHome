using Microsoft.Win32;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    /// <summary>
    /// Implements an adapter wrapper over the native Windows <see cref="RegistryKey"/> structure, 
    /// routing lifecycle configurations, structural node creations, and key evaluations to 
    /// support decoupled mocking frameworks during architectural regression testing.
    /// </summary>
    public class RegistryKeyWrapper : IRegistryKey
    {
        private readonly RegistryKey _registryKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryKeyWrapper"/> class, proxying access 
        /// to a native operating system registry key node.
        /// </summary>
        /// <param name="registryKey">The active native concrete <see cref="RegistryKey"/> resource instance being adapted.</param>
        public RegistryKeyWrapper(RegistryKey registryKey)
        {
            _registryKey = registryKey;
        }

        /// <summary>
        /// Sets the specified name/value pair within the targeted registry key hive location, 
        /// formatting data configurations under explicit value structures.
        /// </summary>
        /// <param name="name">The case-insensitive structural name string of the target value property to set or modify.</param>
        /// <param name="value">The data payload content matching the storage properties required by the tracking key.</param>
        /// <param name="kind">The explicit native OS registry storage format flag (e.g., DWORD, Binary, ExpandString).</param>
        public void SetValue(string name, object value, RegistryValueKind kind)
        {
            _registryKey.SetValue(name, value, kind);
        }

        /// <summary>
        /// Retrieves the data payload contents associated with a specified name signature 
        /// from the targeted configuration path block.
        /// </summary>
        /// <param name="name">The case-insensitive structural name string of the target value property to look up.</param>
        /// <returns>The unmapped raw data object configuration if located; otherwise, <c>null</c>.</returns>
        public object? GetValue(string name)
        {
            return _registryKey.GetValue(name);
        }

        /// <summary>
        /// Removes a named registry value entry tracking attribute out of the active registry subkey structure.
        /// </summary>
        /// <param name="name">The case-insensitive name string identifier of the value property to delete.</param>
        public void DeleteValue(string name)
        {
            _registryKey.DeleteValue(name);
        }

        /// <summary>
        /// Establishes an active reading or writing stream connection session over an existing structural 
        /// subdirectory key matching the specified name criteria.
        /// </summary>
        /// <param name="name">The localized subkey name path sequence segment to open from this node root.</param>
        /// <param name="writable">A flag which, when <c>true</c>, requests elevated write mutation permissions over the targeted subkey path.</param>
        /// <returns>An encapsulated <see cref="IRegistryKey"/> wrapper reference block if the targeted path structure exists safely; otherwise, <c>null</c>.</returns>
        public IRegistryKey? OpenSubKey(string name, bool writable)
        {
            var subKey = _registryKey.OpenSubKey(name, writable);
            return subKey == null ? null : new RegistryKeyWrapper(subKey);
        }

        /// <summary>
        /// Creates a new subkey structure segment or opens an existing match node point path 
        /// while instantly establishing the specified access write rules.
        /// </summary>
        /// <param name="name">The structural subkey path name string sequence to build inside the current registry branch node.</param>
        /// <param name="writable">A flag indicating whether write mutations are permitted on the returned subkey context.</param>
        /// <returns>The newly created or established <see cref="IRegistryKey"/> subkey node wrapper instance block pointer.</returns>
        public IRegistryKey? CreateSubKey(string name, bool writable)
        {
            var subKey = _registryKey.CreateSubKey(name, writable);
            return subKey == null ? null : new RegistryKeyWrapper(subKey);
        }

        /// <summary>
        /// Releases all underlying unmanaged OS storage handles and registry lock state references 
        /// held active by the encapsulated native registry resource components.
        /// </summary>
        public void Dispose()
        {
            _registryKey.Dispose();
        }
    }
}