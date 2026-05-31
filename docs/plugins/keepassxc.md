# keepassxc plugin

## Description

The `keepassxc` plugin manages KeePassXC application settings on Windows. It merges INI-style section/key pairs into `keepassxc.ini`, preserving comments, line endings, and unknown lines where possible.

## Prerequisites

- KeePassXC must be installed ([keepassxc.org](https://keepassxc.org))
- KeePassXC is detected via `KeePassXC.exe` on `PATH` or under `%ProgramFiles%\KeePassXC` / `%LOCALAPPDATA%\KeePassXC`
- `APPDATA` must be set (standard on Windows)
- **Windows only**

## Configuration file location

| Platform | Path |
|----------|------|
| Windows  | `%APPDATA%\KeePassXC\keepassxc.ini` |

## Configuration format

```yaml
extensions:
  keepassxc:
    settings:
      <SectionName>:
        <Key>: <value>
```

Each top-level key under `settings` is an INI section (written as `[SectionName]`). Nested keys become `Key=value` lines within that section.

## Supported settings

Any valid KeePassXC INI section and key is supported. Common sections include:

| Section | Key | Type | Description |
|---------|-----|------|-------------|
| `General` | `AutoSaveAfterEveryChange` | boolean | Save database after each edit |
| `General` | `MinimizeOnStartup` | boolean | Start minimized to tray |
| `GUI` | `Theme` | string | UI theme name |
| `GUI` | `Language` | string | UI language code |
| `Security` | `ClearClipboard` | integer | Seconds before clearing clipboard |
| `Browser` | `Enabled` | boolean | Enable browser integration |

Boolean values are written as `true` / `false` strings.

## Usage examples

### General preferences

```yaml
extensions:
  keepassxc:
    settings:
      General:
        AutoSaveAfterEveryChange: true
        MinimizeOnStartup: false
```

### GUI theme

```yaml
extensions:
  keepassxc:
    settings:
      GUI:
        Theme: dark
```

### Security clipboard timeout

```yaml
extensions:
  keepassxc:
    settings:
      Security:
        ClearClipboard: 30
```

## Notes

- Missing sections are created; existing keys in a section are updated in place when the key name matches (case-insensitive).
- Corrupt or unreadable files are backed up with a `.corrupted.<timestamp>.<suffix>` suffix.
- Config directory is created with mode `0700` when needed.
- Supports `dryRun` mode — logs the target path without writing.
- Config key in WinHome: `extensions.keepassxc`.
- Restart KeePassXC for some GUI settings to apply.

## Verification

After applying:

```powershell
Get-Content "$env:APPDATA\KeePassXC\keepassxc.ini"
```

Confirm your `[Section]` blocks contain the expected keys, then open KeePassXC and verify the setting in the UI.
