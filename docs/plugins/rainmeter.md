# Rainmeter Plugin

## Overview

The Rainmeter plugin manages global Rainmeter settings stored in `Rainmeter.ini` by merging
configured sections and key-value pairs into that file. It uses a custom INI parser
(`configparser` with case preserved) so existing sections and skins already present in the file
are left untouched — only the sections and keys you specify in `config.yaml` are added or updated.

## Prerequisites

- Windows, with the `APPDATA` environment variable available
- Rainmeter installed (the plugin detects it via `Rainmeter.exe`/`Rainmeter` in `PATH`, or at the
  default `%PROGRAMFILES%\Rainmeter\Rainmeter.exe` install path)

## Configuration Schema

The plugin accepts a top-level YAML object with one supported field:

| Field      | Type   | Default | Description                                                                                   |
| :--------- | :----- | :------ | :---------------------------------------------------------------------------------------------- |
| `settings` | object | none    | A dictionary where keys are INI sections (e.g. `Rainmeter`) and values are key-value pairs for that section. |

Any section/key pair accepted by `Rainmeter.ini` can be used. The most common section is
`[Rainmeter]`, which holds global options such as:

| Key | Purpose |
| --- | ------- |
| `ConfigEditor` | Path to the text editor used for "Edit Skin" / "Edit Settings" (default: Notepad) |
| `SkinPath` | Fully qualified path to the skins folder |
| `TrayIcon` | Set to `0` to hide the notification area icon |
| `DisableVersionCheck` | Set to `1` to stop Rainmeter checking for updates |
| `Logging` | Set to `1` to enable logging |
| `Debug` | Set to `1` to enable debug logging |
| `DisableDragging` | Set to `1` to prevent skins from being dragged |
| `Language` | Locale ID used for the Rainmeter UI |

The plugin isn't limited to `[Rainmeter]` — any other section name (e.g. a skin's own section like
`Skin\MySkin\MyConfig`) is written the same way.

The plugin also supports WinHome's `dryRun` apply option, which reports whether `Rainmeter.ini`
would change without writing it.

## Usage Examples

### Example 1 — Set config editor and skin path

```yaml
extensions:
  rainmeter:
    settings:
      Rainmeter:
        ConfigEditor: 'C:\Program Files\Notepad++\notepad++.exe'
        SkinPath: 'C:\Users\me\Documents\Rainmeter\Skins'
```

### Example 2 — Disable update checks and tray icon

```yaml
extensions:
  rainmeter:
    settings:
      Rainmeter:
        DisableVersionCheck: 1
        TrayIcon: 0
        Logging: 0
```

### Example 3 — Configure a specific skin section

```yaml
extensions:
  rainmeter:
    settings:
      Skin\illustro\Clock:
        Active: 1
        Draggable: 1
        AlwaysOnTop: -1
```

## Verification Steps

```powershell
Test-Path "$env:APPDATA\Rainmeter\Rainmeter.ini"
Get-Content "$env:APPDATA\Rainmeter\Rainmeter.ini"
```

Confirm the sections/keys you configured appear with the expected values. Since the plugin only
writes the INI file and doesn't restart or signal Rainmeter, fully reload Rainmeter (or use
**Manage → Refresh all**) afterward to confirm the running application picks up the change.

## Notes / Caveats

- This plugin targets Windows only — it resolves Rainmeter's config through `%APPDATA%` and checks
  for `Rainmeter.exe` using Windows-style paths.
- The INI file is written atomically: a temp file is written first, then swapped into place with
  `os.replace`, so a crash mid-write won't corrupt the existing file.
- If the existing `Rainmeter.ini` can't be parsed (corrupted), the plugin backs it up to
  `Rainmeter.ini.<random-uuid>.bak` before writing a fresh file from your `settings`.
- The plugin writes the file but does not restart Rainmeter or trigger a refresh — you'll need to
  do that yourself to see changes take effect in the running app.
- `check_installed` only checks `PATH` and the default `%PROGRAMFILES%\Rainmeter` location; custom
  install directories outside of `PATH` won't be detected.
