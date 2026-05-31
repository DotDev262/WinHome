# Postman plugin

## Description

The `postman` plugin manages Postman API client settings stored as JSON under the user's Postman data directory. It deep-merges WinHome `settings` into the active config file, preserving unknown keys.

## Prerequisites

- Postman installed on Windows
- Detected when `%APPDATA%\Postman\` exists
- `APPDATA` must be set

## Configuration file location

The plugin auto-discovers the newest JSON settings file under:

| Priority | Path |
|----------|------|
| 1 | `%APPDATA%\Postman\packages\**\settings.json` (and `userSettings.json` / `config.json`) |
| 2 | `%APPDATA%\Postman\storage\settings.json` |
| 3 | `%APPDATA%\Postman\packages\postman-settings\settings.json` (created when missing) |

## Supported settings

Common flat keys include:

| Key | Type | Description |
|-----|------|-------------|
| `theme` | string | UI theme (`light` / `dark`) |
| `fontSize` | int | Editor font size |
| `twoPaneView` | bool | Two-pane layout |
| `sendAndDownload` | bool | Send and download button |
| `requestTimeout` | int | Request timeout (ms) |
| `autoCloseRequest` | bool | Auto-close requests |
| `trimKeys` | bool | Auto-trim keys in editor |
| `alwaysShowVariableValues` | bool | Show resolved variable values |
| `sslVerification` | bool | Enable SSL verification |

Nested objects are deep-merged.

## Usage examples

### Editor defaults

```yaml
extensions:
  postman:
    settings:
      theme: dark
      fontSize: 16
      twoPaneView: true
```

### Request behavior

```yaml
extensions:
  postman:
    settings:
      requestTimeout: 30000
      sslVerification: true
      autoCloseRequest: false
```

## Notes

- Unknown keys in the existing JSON file are preserved.
- Corrupted files are backed up with a `.corrupted.<timestamp>` suffix before rewrite.
- Writes are atomic via `tempfile.mkstemp()` and `os.replace()`.
- Supports `dryRun`.
- Config key in WinHome: `extensions.postman`.

## Verification

After applying:

```powershell
Get-Content "$env:APPDATA\Postman\storage\settings.json" -ErrorAction SilentlyContinue
# Or search packages:
Get-ChildItem "$env:APPDATA\Postman\packages" -Recurse -Filter settings.json
```

Restart Postman and confirm preferences under **Settings**.
