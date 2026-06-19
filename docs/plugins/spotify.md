# Spotify Plugin

## Overview

The Spotify plugin manages the Spotify desktop client `prefs` file by merging configured key-value
settings into `%APPDATA%\Spotify\prefs`.

## Prerequisites

- Windows with the `APPDATA` environment variable available
- Spotify desktop client installed or a writable `%APPDATA%\Spotify` directory

## Configuration Schema

The plugin accepts a top-level YAML object with one supported configuration field:

| Field | Type | Default | Description |
| ----- | ---- | ------- | ----------- |
| `settings` | object | none | Spotify `prefs` keys and values to merge into the preferences file. |

Supported settings are any key-value pairs accepted by Spotify's `prefs` file. WinHome preserves the
key names exactly as provided and writes each value as a string. The plugin also supports WinHome's
`dryRun` apply option, which reports whether the `prefs` file would change without writing it.

## Usage Examples

### Set high playback quality

```yaml
extensions:
  spotify:
    settings:
      audio.play_bitrate_enumeration: "5"
```

### Disable track notifications

```yaml
extensions:
  spotify:
    settings:
      ui.track_notifications_enabled: "false"
```

### Configure playback and notifications together

```yaml
extensions:
  spotify:
    settings:
      audio.play_bitrate_enumeration: "5"
      ui.track_notifications_enabled: "true"
```

## Verification Steps

```powershell
Test-Path "$env:APPDATA\Spotify\prefs"
Get-Content "$env:APPDATA\Spotify\prefs"
```

## Notes / Caveats

- This plugin currently targets Windows because it resolves Spotify settings through `%APPDATA%`.
- Values are written as strings in `key=value` format.
- The plugin updates the `prefs` file atomically by writing a temporary file and replacing the
  original.
