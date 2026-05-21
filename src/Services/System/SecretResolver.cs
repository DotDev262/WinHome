using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    /// <summary>
    /// Implements a sophisticated configuration resolver that scans objects for placeholder syntax 
    /// (e.g., <c>{{ provider:key }}</c>) and dynamically substitutes them with secrets retrieved from 
    /// environment variables, filesystem paths, or the Windows Credential Manager vault.
    /// </summary>
    public class SecretResolver : ISecretResolver
    {
        private readonly ILogger _logger;
        
        // Regex to match {{ provider:key }}
        // Captures: 1=provider, 2=key
        private static readonly Regex SecretPattern = new Regex(@"{{\s*(\w+):(.+?)\s*}}", RegexOptions.Compiled);

        // P/Invoke declarations for Windows Credential Manager
        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public long LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretResolver"/> class.
        /// </summary>
        /// <param name="logger">The diagnostic logging utility used to record resolution warnings or failures.</param>
        public SecretResolver(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parses a string for secret placeholders and replaces them with resolved values.
        /// </summary>
        /// <param name="input">The raw string containing potential secret placeholders.</param>
        /// <returns>The interpolated string with resolved values.</returns>
        public string Resolve(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return SecretPattern.Replace(input, match =>
            {
                string provider = match.Groups[1].Value.ToLower();
                string key = match.Groups[2].Value.Trim();

                try
                {
                    return provider switch
                    {
                        "env" => ResolveEnv(key),
                        "file" => ResolveFile(key),
                        "vault" => ResolveVault(key),
                        _ => match.Value // Return original if unknown provider
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Secret] Failed to resolve {match.Value}: {ex.Message}");
                    return match.Value;
                }
            });
        }

        /// <summary>
        /// Recursively traverses an object (including Lists and Dictionaries) to resolve secret placeholders in string properties.
        /// </summary>
        /// <param name="obj">The target object to perform in-place resolution upon.</param>
        public void ResolveObject(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();

            // Handle Lists
            if (obj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item is string str) list[i] = Resolve(str);
                    else if (item != null && !item.GetType().IsPrimitive) ResolveObject(item);
                }
                return;
            }

            // Handle Dictionaries
            if (obj is IDictionary dict)
            {
                var keys = new List<object>();
                foreach (var k in dict.Keys) keys.Add(k);

                foreach (var key in keys)
                {
                    var value = dict[key];
                    if (value is string strVal) dict[key] = Resolve(strVal);
                    else if (value != null && !value.GetType().IsPrimitive) ResolveObject(value);
                }
                return;
            }

            // Handle Properties
            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;

                var value = prop.GetValue(obj);
                if (value == null) continue;

                if (prop.PropertyType == typeof(string) && prop.CanWrite)
                {
                    prop.SetValue(obj, Resolve((string)value));
                }
                else if (!prop.PropertyType.IsPrimitive && !prop.PropertyType.IsEnum && prop.PropertyType != typeof(string))
                {
                    ResolveObject(value);
                }
            }
        }

        private string ResolveEnv(string variable)
        {
            var val = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrEmpty(val))
            {
                _logger.LogWarning($"[Secret] Environment variable '{variable}' not found.");
                return string.Empty;
            }
            return val;
        }

        private string ResolveVault(string targetName)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogWarning("[Secret] Vault provider is only supported on Windows.");
                return string.Empty;
            }

            uint[] credTypes = { 1, 2 }; // Generic (1) and Domain Password (2)
            foreach (var credType in credTypes)
            {
                if (!CredRead(targetName, credType, 0, out IntPtr credPtr)) continue;

                try
                {
                    var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                    if (cred.CredentialBlobSize == 0 || cred.CredentialBlob == IntPtr.Zero) return string.Empty;

                    return Marshal.PtrToStringUni(cred.CredentialBlob, (int)(cred.CredentialBlobSize / sizeof(char)));
                }
                finally
                {
                    CredFree(credPtr);
                }
            }

            _logger.LogWarning($"[Secret] Credential '{targetName}' not found in Windows Credential Manager.");
            return string.Empty;
        }

        private string ResolveFile(string path)
        {
            if (path.StartsWith("~"))
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.TrimStart('~', '/', '\\'));
            }

            if (!File.Exists(path))
            {
                _logger.LogWarning($"[Secret] File '{path}' not found.");
                return string.Empty;
            }

            return File.ReadAllText(path).Trim();
        }
    }
}