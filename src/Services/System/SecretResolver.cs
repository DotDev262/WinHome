using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
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

        public SecretResolver(ILogger logger)
        {
            _logger = logger;
        }

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
                    if (item is string str)
                    {
                        list[i] = Resolve(str);
                    }
                    else if (item != null && !item.GetType().IsPrimitive)
                    {
                        ResolveObject(item);
                    }
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
                    if (value is string strVal)
                    {
                        dict[key] = Resolve(strVal);
                    }
                    else if (value != null && !value.GetType().IsPrimitive)
                    {
                        ResolveObject(value);
                    }
                }
                return;
            }

            // Handle Properties
            foreach (var prop in type.GetProperties())
            {
                if (!prop.CanRead) continue;

                // Skip indexers
                if (prop.GetIndexParameters().Length > 0) continue;

                var value = prop.GetValue(obj);
                if (value == null) continue;

                if (prop.PropertyType == typeof(string) && prop.CanWrite)
                {
                    var resolved = Resolve((string)value);
                    prop.SetValue(obj, resolved);
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

            // Try CRED_TYPE_GENERIC (1) first, then CRED_TYPE_DOMAIN_PASSWORD (2) used by cmdkey
            uint[] credTypes = { 1, 2 };
            foreach (var credType in credTypes)
            {
                if (!CredRead(targetName, credType, 0, out IntPtr credPtr))
                    continue;

                try
                {
                    var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                    if (cred.CredentialBlobSize == 0 || cred.CredentialBlob == IntPtr.Zero)
                        return string.Empty;

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
            // Handle ~
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
