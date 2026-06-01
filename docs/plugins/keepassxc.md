# KeePassXC Plugin

## Overview

KeePassXC is a very popular, secure, and offline password manager. All its settings (such as setting
a dark theme, connecting to a browser, or how long it takes to lock the database) are saved in an
INI file on Windows located at %APPDATA%\KeePassXC\keepassxc.ini. Users don't have to dig through
and edit that .ini file every time. They can simply write their KeePassXC settings to WinHome's
central config.yaml.This plugin's Python code includes a custom INI parser. It reads the native .ini
file and updates only the new settings, without preserving any previous comments (containing # or ;)
or empty lines. If a new section (such as [GUI]) doesn't already exist, it automatically creates
it.The best feature is that if the user's original .ini file format is accidentally corrupted, this
plugin doesn't crash. It silently creates a backup of the corrupted file (e.g.
keepassxc.ini.corrupted.20260528...) and finishes its work by creating a fresh file, so that the
user's data remains safe.

## Prerequisites

- The user must ensure that KeePassXC is installed on their system.
- The plugin automatically detects the installation by looking for `KeePassXC.exe` or `keepassxc` in
  the system PATH, or by checking default installation paths like
  `C:\Program Files\KeePassXC\KeePassXC.exe` and `%LOCALAPPDATA%\KeePassXC\KeePassXC.exe`.

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                                                         |
| :--------- | :----- | :------ | :------------------------------------------------------------------------------------------------------------------ |
| `settings` | object | none    | A dictionary where keys are INI sections (e.g., `GUI`, `Security`) and values are key-value pairs for that section. |

## Usage Examples

### Example 1 — Configure GUI and Security settings

```yaml
extensions:
  keepassxc:
    settings:
      GUI:
        ApplicationTheme: 'dark'
        MinimizeOnClose: true
        MinimizeToTray: true
      Security:
        ClearClipboardTimeout: 10
        LockDatabaseIdle: true
        LockDatabaseIdleSeconds: 300
```

### Example 2 — Enable Browser Integration

```yaml
extensions:
  keepassxc:
    settings:
      Browser:
        Enabled: true
        CustomProxyLocation: ''
        UpdatePasswords: true
```

### Example 3 — Configure Password Generator defaults

```yaml
extensions:
  keepassxc:
    settings:
      PasswordGenerator:
        Length: 20
        UseNumbers: true
        UseSpecialChars: true
        UseLowerCase: true
        UseUpperCase: true
```

## Notes / Caveats

- The plugin features a robust custom INI parser that safely merges new values while preserving
  existing comments, empty lines, and formatting in the native file.
- If a specified section or key does not exist in the native config, the plugin will create it
  automatically.
- If the native `keepassxc.ini` file is unreadable or corrupted, the plugin automatically creates a
  safe backup (appended with `.corrupted.<timestamp>`) before starting fresh to prevent data loss.
- It supports `dryRun` mode — when it is enabled, it logs what changes would be made to the config
  file without actually writing them to the disk.
- The plugin safely merges new values into the INI file using a custom parser.
- Existing comments, empty lines, and unmanaged keys are preserved.
- Missing sections or keys are automatically created.

## Verification Steps

After applying, the user can verify if KeePassXC has picked up the changes by opening the KeePassXC
application and checking the Settings menu. Or by inspecting the INI file:

```bash
cat $env:APPDATA\KeePassXC\keepassxc.ini
```
