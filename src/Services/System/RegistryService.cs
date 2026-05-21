using System;
using Microsoft.Win32;
using System.Runtime.Versioning;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Infrastructure.Helpers;

namespace WinHome.Services.System
{
    /// <summary>
    /// Provides administrative manipulation primitives over the Windows registry subsystem, 
    /// abstracting key creations, state comparisons, property mutations, and target reversions 
    /// through isolated abstraction drivers.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class RegistryService : IRegistryService
    {
        private readonly IRegistryWrapper _registryWrapper;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryService"/> class with decoupled registry engine providers.
        /// </summary>
        /// <param name="registryWrapper">The interface wrapper utility used to resolve native root registry keys safely.</param>
        /// <param name="logger">The diagnostic log tracker capture utility routing system statuses or errors.</param>
        public RegistryService(IRegistryWrapper registryWrapper, ILogger logger)
        {
            _registryWrapper = registryWrapper;
            _logger = logger;
        }

        /// <summary>
        /// Evaluates and executes a targeted state modification parameter tweak inside the Windows registry hive.
        /// </summary>
        /// <param name="tweak">The structural data object containing target paths, property names, assignments, and storage types.</param>
        /// <param name="dryRun">A conditional flag which, when <c>true</c>, logs intent without altering registry states on disk.</param>
        /// <exception cref="InvalidOperationException">Rethrown explicitly when a critical context verification boundary is violated via security rules.</exception>
        public void Apply(RegistryTweak tweak, bool dryRun)
        {
            try
            {
                // Security Check: Prevent HKCU modification when running within a non-interactive SYSTEM execution context
                RegistryGuard.ValidateContext(tweak.Path);

                IRegistryKey root = _registryWrapper.GetRootKey(tweak.Path, out string subKeyPath);

                using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: false))
                {
                    object? currentValue = key?.GetValue(tweak.Name);

                    // Idempotency: Avoid unnecessary disk write cycles if the value matches the configuration target
                    if (currentValue != null && currentValue.ToString() == tweak.Value?.ToString())
                    {
                        _logger.LogSuccess($"[Registry] Skipped: {tweak.Name} (Already set)");
                        return;
                    }

                    if (dryRun)
                    {
                        _logger.LogError($"[DryRun] Would set Registry: {tweak.Path}\\{tweak.Name} = {tweak.Value}");
                        return;
                    }
                }

                using (IRegistryKey? key = root.CreateSubKey(subKeyPath, writable: true))
                {
                    if (key == null)
                    {
                        _logger.LogError($"[Error] Could not create registry subkey: {tweak.Path}");
                        return;
                    }

                    RegistryValueKind kind = tweak.Type.ToLower() switch
                    {
                        "dword" => RegistryValueKind.DWord,
                        "qword" => RegistryValueKind.QWord,
                        "binary" => RegistryValueKind.Binary,
                        _ => RegistryValueKind.String
                    };

                    object? valueToWrite = tweak.Value;
                    
                    // Unpack and normalize underlying System.Text.Json configuration variants safely
                    if (valueToWrite is global::System.Text.Json.JsonElement jsonElement)
                    {
                        if (kind == RegistryValueKind.DWord) valueToWrite = jsonElement.GetInt32();
                        else if (kind == RegistryValueKind.QWord) valueToWrite = jsonElement.GetInt64();
                        else valueToWrite = jsonElement.ToString() ?? string.Empty;
                    }
                    else
                    {
                        if (kind == RegistryValueKind.DWord) valueToWrite = Convert.ToInt32(tweak.Value);
                        else if (kind == RegistryValueKind.QWord) valueToWrite = Convert.ToInt64(tweak.Value);
                    }

                    key.SetValue(tweak.Name, valueToWrite ?? string.Empty, kind);
                    _logger.LogSuccess($"[Registry] Set {tweak.Name} = {tweak.Value}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] Registry apply failed: {ex.Message}");
                
                // Ensure critical contextual security warnings escape generic block captures
                if (ex is InvalidOperationException && ex.Message.StartsWith("Security Risk"))
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Reverts a targeted registry adjustment parameter string entry by removing the target property key from the registry tree.
        /// </summary>
        /// <param name="path">The fully qualified hive directory layout location containing the variable flag target.</param>
        /// <param name="name">The explicit key property identity flag sequence to clear from the target node.</param>
        /// <param name="dryRun">A conditional flag which, when <c>true</c>, simulates target deletion actions to console logs.</param>
        public void Revert(string path, string name, bool dryRun)
        {
            try
            {
                // Security Check
                RegistryGuard.ValidateContext(path);

                IRegistryKey root = _registryWrapper.GetRootKey(path, out string subKeyPath);
                using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: !dryRun))
                {
                    if (key == null) return;

                    if (key.GetValue(name) != null)
                    {
                        if (dryRun)
                        {
                            _logger.LogError($"[DryRun] Would delete Registry value: {path}\\{name}");
                            return;
                        }

                        key.DeleteValue(name);
                        _logger.LogSuccess($"[Registry] Reverted {name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] Registry revert failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Queries an isolated configuration segment out of the active Windows registry subkey mapping tree path.
        /// </summary>
        /// <param name="path">The fully qualified hive directory layout location containing the variable flag target.</param>
        /// <param name="name">The explicit key property identity flag sequence being searched.</param>
        /// <returns>The unmapped payload object tracked by the operating system repository if found; otherwise, <c>null</c>.</returns>
        public object? Read(string path, string name)
        {
            try
            {
                IRegistryKey root = _registryWrapper.GetRootKey(path, out string subKeyPath);
                using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: false))
                {
                    return key?.GetValue(name);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}