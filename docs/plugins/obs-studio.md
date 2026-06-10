# OBS Studio Plugin

## Overview

The OBS Studio plugin manages global and profile-specific configuration files for the OBS Studio broadcasting/recording app.

## Prerequisites

- OBS Studio installed (executable at `%PROGRAMFILES%\obs-studio\bin\64bit\obs64.exe` or custom location configured in `OBS_INSTALL_DIR`)

## Configuration Schema

The plugin modifies configurations in `%APPDATA%\obs-studio`. The available keys are:

| Key | Section | Description |
|-----|---------|-------------|
| `general` | Global | Dict containing options for `global.ini` under `[General]` (e.g. `theme`, `language`, `prevents_display_sleep`). |
| `profile` | Profiles | Explicit profile directory name to update. If omitted, the active profile directory is used. |
| `video` | Profile | Dict containing options for the profile's `basic.ini` under `[Video]` (e.g. `base_resolution`, `output_resolution`, `fps_type`, `fps_common`). |
| `audio` | Profile | Dict containing options for the profile's `basic.ini` under `[Audio]` (e.g. `sample_rate`, `channels`, `desktop_device`, `mic_device`). |
| `output` | Profile | Dict containing output and mode settings mapped to `[Output]` and `[SimpleOutput]` (recording and streaming bitrate/encoders). |
| `hotkeys` | Profile | Dict containing key mappings to merge into `basic.ini` under `[Hotkeys]`. |
| `profiles` | Profiles | List of profile objects containing `name` and their corresponding `settings` dictionary. |

## Usage Examples

```yaml
plugins:
  - name: obs-studio
    general:
      theme: "Yami"
      language: "en-US"
    video:
      base_resolution: "1920x1080"
      output_resolution: "1920x1080"
      fps_common: "60"
    audio:
      sample_rate: 48000
      channels: "Stereo"
    output:
      mode: "Simple"
      recording:
        path: "D:\\Recordings"
        quality: "HQ"
        format: "mp4"
```

## Verification Steps

To verify OBS Studio installation state:
```bash
# Check if OBS is found
obs64.exe --version
```
To verify settings, inspect global and profile configuration INI files:
```bash
cat %APPDATA%\obs-studio\global.ini
cat %APPDATA%\obs-studio\basic\profiles\<profile_name>\basic.ini
```

## Notes / Caveats

- Settings are deep merged section-by-section.
- If a configuration file is corrupted or failed to parse, the plugin automatically creates a timestamped backup copy and initializes a clean configuration.
