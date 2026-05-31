# VLC plugin

## Description

The `vlc` plugin manages VLC media player settings in `%APPDATA%\vlc\vlcrc`. It merges WinHome `settings` into VLC's INI-like config file, preserving comments, unknown keys, and duplicate keys used for multi-value options.

## Prerequisites

- VLC installed on Windows
- Detected when `%APPDATA%\vlc\` exists or `vlc.exe` is on `PATH`
- `APPDATA` must be set

## Configuration file location

| Platform | Path |
|----------|------|
| Windows  | `%APPDATA%\vlc\vlcrc` |

## Configuration format

Global VLC options are flat keys under `settings`. Nested objects map to INI sections:

```yaml
extensions:
  vlc:
    settings:
      volume: 256
      network-caching: 1000
      video-on-top: true
```

Multi-value keys (same key repeated in `vlcrc`) accept YAML lists:

```yaml
extensions:
  vlc:
    settings:
      enable-lua-sd:
        - bonjour
        - podcast
```

## Supported settings

Common keys include:

| Key | Type | Description |
|-----|------|-------------|
| `snapshot-path` | string | Screenshot save directory |
| `snapshot-format` | string | `png`, `jpg`, or `tiff` |
| `network-caching` | int | Network cache (ms) |
| `file-caching` | int | File cache (ms) |
| `volume` | int | Default volume (0–512) |
| `video-on-top` | bool | Always-on-top video window |
| `aspect-ratio` | string | e.g. `16:9` |
| `sub-language` | string | Subtitle language code |
| `audio-language` | string | Audio track language |
| `recent-playlist` | int | Max recent playlist items |
| `qt-max-volume` | int | Max volume slider percentage |
| `http-proxy` | string | HTTP proxy URL |
| `enable-lua-sd` | string or list | Lua service-discovery modules |
| `playlist-cork` | bool | Auto-pause on phone call |

Boolean values are written as `1` / `0`, matching VLC conventions.

## Usage examples

### Playback defaults

```yaml
extensions:
  vlc:
    settings:
      volume: 256
      network-caching: 1500
      file-caching: 600
```

### Screenshot preferences

```yaml
extensions:
  vlc:
    settings:
      snapshot-format: png
      snapshot-path: "C:\\Users\\me\\Pictures\\VLC"
```

### Service discovery modules

```yaml
extensions:
  vlc:
    settings:
      enable-lua-sd:
        - bonjour
        - podcast
```

## Notes

- Unknown keys and sections in an existing `vlcrc` are preserved.
- Corrupted files are backed up with a `.corrupted.<timestamp>` suffix before rewrite.
- Writes are atomic via a temp file and `os.replace()`.
- Supports `dryRun`.
- Config key in WinHome: `extensions.vlc`.

## Verification

After applying:

```powershell
Get-Content "$env:APPDATA\vlc\vlcrc" | Select-String volume
# Restart VLC and confirm preferences under Tools → Preferences
```
