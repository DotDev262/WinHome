# Windows Explorer Plugin

## Overview

The Windows Explorer plugin manages advanced registry configurations for the Windows File Explorer.

## Prerequisites

- Read/write access to the current user's registry hive (`HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`)

## Configuration Schema

Settings accept key-value pairs under the `settings` key. Mapped boolean options are converted to registry DWORD values (`true` to `1`, `false` to `0`):

| Key | Registry Key | Description |
|-----|--------------|-------------|
| `HideFileExt` | `HideFileExt` | Hides file extensions for known file types. |
| `ShowSuperHidden` | `ShowSuperHidden` | Displays protected operating system files. |
| `ShowSyncProviderNotification`| `ShowSyncProviderNotification` | Shows sync provider notifications. |
| `ShowStatusBar` | `ShowStatusBar` | Shows status bar in File Explorer. |
| `AutoCheckSelect` | `AutoCheckSelect` | Displays check boxes to select items. |
| `DisableThumbnails` | `DisableThumbnails` | Shows icons instead of file thumbnails. |
| `DisableThumbsDBOnNetworkFolders` | `DisableThumbsDBOnNetworkFolders` | Disables caching of thumbnails in network folders. |
| `SeparateProcess` | `SeparateProcess` | Launches folder windows in a separate process. |
| `Hidden` | `Hidden` | Configures visibility of hidden files. Accepts `1` (show hidden files) or `2` (do not show hidden files). |

## Usage Examples

```yaml
plugins:
  - name: windows-explorer
    settings:
      HideFileExt: false
      ShowSuperHidden: true
      Hidden: 1
```

## Verification Steps

To verify the registry settings:
```powershell
Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
```

## Notes / Caveats

- Unmapped keys specified in settings are ignored safely.
- A restart or refresh of `explorer.exe` may be required for some registry options to take visual effect in the shell UI.
