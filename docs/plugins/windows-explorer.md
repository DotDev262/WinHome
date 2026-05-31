# Windows Explorer plugin

## Description

The `windows-explorer` plugin manages File Explorer folder view settings stored in the current-user registry hive.

## Configuration source

| Item | Value |
|------|-------|
| Hive | `HKCU` |
| Path | `Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` |
| API | Python `winreg` (stdlib) |

No administrator rights are required.

## Supported settings

| Key | Type | Description |
|-----|------|-------------|
| `Hidden` | int | Show hidden files (`1` = hide, `2` = show) |
| `HideFileExt` | bool | Hide known file extensions |
| `ShowSuperHidden` | bool | Show protected operating system files |
| `ShowSyncProviderNotification` | bool | Show sync provider notifications |
| `ShowStatusBar` | bool | Show status bar |
| `AutoCheckSelect` | bool | Auto-check select checkboxes |
| `DisableThumbnails` | bool | Disable thumbnail caching |
| `DisableThumbsDBOnNetworkFolders` | bool | Disable `thumbs.db` on network folders |
| `SeparateProcess` | bool | Launch folder windows in a separate process |

## Usage examples

### Show hidden files and extensions

```yaml
extensions:
  windows-explorer:
    settings:
      Hidden: 2
      HideFileExt: false
      ShowSuperHidden: false
```

### Explorer UI preferences

```yaml
extensions:
  windows-explorer:
    settings:
      ShowStatusBar: true
      SeparateProcess: true
      DisableThumbnails: false
```

## Notes

- `check_installed` always returns `true` because File Explorer is built into Windows.
- Boolean settings are stored as `REG_DWORD` values (`0` / `1`).
- Supports `dryRun` in request context.
- Config key in WinHome: `extensions.windows-explorer`.

## Verification

```powershell
Get-ItemProperty HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced |
  Select-Object Hidden, HideFileExt, ShowStatusBar
```

Restart Explorer or sign out for some UI changes to take effect.
