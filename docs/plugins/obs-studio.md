# OBS Studio Plugin

## Overview

This plugin manages OBS Studio configuration files stored in `%APPDATA%\obs-studio`. It allows users to configure general settings, video settings, audio settings, output settings, hotkeys, and profiles through WinHome.

## Prerequisites

* OBS Studio installed
* Windows system with `%APPDATA%` available
* Permission to write to `%APPDATA%\obs-studio`

## Configuration File Location

| Platform | Path                                                      |
| -------- | --------------------------------------------------------- |
| Windows  | `%APPDATA%\obs-studio\global.ini`                         |
| Windows  | `%APPDATA%\obs-studio\basic\profiles\<profile>\basic.ini` |

## Configuration Schema

The plugin supports the following top-level fields:

| Field      | Type   | Description                           |
| ---------- | ------ | ------------------------------------- |
| `general`  | object | Global OBS settings                   |
| `profile`  | string | Profile to configure                  |
| `video`    | object | Video configuration                   |
| `audio`    | object | Audio configuration                   |
| `output`   | object | Recording and streaming configuration |
| `hotkeys`  | object | Hotkey mappings                       |
| `profiles` | list   | Create or manage OBS profiles         |

### Supported General Settings

| Key                      | Description                                     |
| ------------------------ | ----------------------------------------------- |
| `theme`                  | OBS theme                                       |
| `language`               | Interface language                              |
| `prevents_display_sleep` | Prevent display sleep while streaming/recording |

### Supported Video Settings

| Key                 | Description            |
| ------------------- | ---------------------- |
| `base_resolution`   | Base canvas resolution |
| `output_resolution` | Output resolution      |
| `fps_type`          | FPS mode               |
| `fps_common`        | Common FPS value       |

### Supported Audio Settings

| Key              | Description           |
| ---------------- | --------------------- |
| `sample_rate`    | Audio sample rate     |
| `channels`       | Channel configuration |
| `desktop_device` | Desktop audio device  |
| `mic_device`     | Microphone device     |

## Usage Examples

### Configure general settings

```yaml
extensions:
  obs-studio:
    general:
      theme: Dark
      language: en-US
      prevents_display_sleep: true
```

### Configure video and audio

```yaml
extensions:
  obs-studio:
    profile: Default
    video:
      base_resolution: 1920x1080
      output_resolution: 1280x720
      fps_common: 60

    audio:
      sample_rate: 48000
      channels: Stereo
```

### Configure recording output

```yaml
extensions:
  obs-studio:
    profile: Default
    output:
      mode: Simple
      recording:
        path: D:\Recordings
        format: mkv
        quality: High Quality
```

## Verification Steps

1. Apply your WinHome configuration.
2. Open OBS Studio.
3. Open Settings and verify the configured values.
4. Confirm the expected values are present in `global.ini` or the selected profile's `basic.ini`.

## Notes / Caveats

* Existing configuration values are preserved unless overwritten.
* Unsupported keys are ignored.
* Corrupted configuration files are automatically backed up before recovery.
* Supports dry-run mode.
* Profile-specific settings require a valid OBS profile.
