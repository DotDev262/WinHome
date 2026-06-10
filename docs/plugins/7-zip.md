# 7-Zip Plugin

## Overview

The 7-Zip plugin manages advanced compression and shell context settings for 7-Zip using the Windows registry.

## Prerequisites

- 7-Zip installed (executable `7z.exe` or `7z` in PATH or registry key `HKCU\Software\7-Zip` exists)

## Configuration Schema

Settings accept key-value pairs under the `settings` key. Mapped options are stored under `HKCU\Software\7-Zip` with their corresponding registry types:

| Key | Registry Type | Description |
|-----|---------------|-------------|
| `CompressionLevel` | `REG_DWORD` | Integer between `0` (Store) and `9` (Ultra). |
| `CompressionMethod`| `REG_SZ` | Compression algorithm (e.g. `"LZMA2"`, `"PPMd"`). |
| `EncryptHeaders` | `REG_DWORD` | Boolean controlling context header encryption. |
| `ContextMenu` | `REG_DWORD` | Bitmask for context menu options. |
| `InstallDir` | `REG_SZ` | Path pointing to the 7-Zip installation directory. |

## Usage Examples

```yaml
plugins:
  - name: 7-zip
    settings:
      CompressionLevel: 9
      CompressionMethod: "LZMA2"
      EncryptHeaders: true
```

## Verification Steps

To verify the registry settings:
```powershell
Get-ItemProperty -Path "HKCU:\Software\7-Zip"
```

## Notes / Caveats

- Registry modifications are supported on Windows platforms.
- If a registry read/write fails, a backup of the current registry key is created under `%USERPROFILE%\7zip_registry.corrupted.*.reg`.
