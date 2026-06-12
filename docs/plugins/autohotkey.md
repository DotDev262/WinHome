# AutoHotkey Plugin

## Overview

This plugin manages an AutoHotkey v2 script file. It writes a managed header plus managed blocks for
settings, hotkeys, and hotstrings, then preserves any custom script content outside those managed
sections.

## Prerequisites

- Windows only.
- AutoHotkey v2 installed.
- `script_path` must point to a writable `.ahk` file.
- The plugin checks for `AutoHotkey64.exe` or `AutoHotkey.exe` in `PATH` and in common install
  locations.

## Configuration Schema

The plugin accepts a top-level YAML object with these fields:

| Field         | Type   | Default  | Description                                                                             |
| ------------- | ------ | -------- | --------------------------------------------------------------------------------------- |
| `script_path` | string | required | Path to the AutoHotkey script to manage. Environment variables are expanded before use. |
| `settings`    | object | none     | Optional managed script settings.                                                       |
| `hotkeys`     | object | none     | Map of hotkey triggers to AutoHotkey actions.                                           |
| `hotstrings`  | object | none     | Map of hotstring triggers to replacement text.                                          |

### `settings`

| Field                   | Type    | Default | Description                                                   |
| ----------------------- | ------- | ------- | ------------------------------------------------------------- |
| `persistent`            | boolean | false   | If true, writes `Persistent` into the managed settings block. |
| `detect_hidden_windows` | string  | none    | Writes `DetectHiddenWindows "<value>"` when set.              |
| `icon_tip`              | string  | none    | Writes `TrayTip "<value>"` when set.                          |

### `hotkeys`

Each key is written as the hotkey trigger, and each value is written as the action body inside a
managed block.

### `hotstrings`

Each key is written verbatim as the hotstring trigger text. Single-line replacements are written
inline. Multi-line replacements are written as a parenthesized block.

## Usage Examples

### Minimal hotkey setup

```yaml
script_path: "%USERPROFILE%\\Documents\\AutoHotkey\\main.ahk"
hotkeys:
  '#z': 'Run "https://www.google.com"'
```

### Settings plus hotstrings

```yaml
script_path: "%USERPROFILE%\\Documents\\AutoHotkey\\main.ahk"
settings:
  persistent: true
  detect_hidden_windows: On
  icon_tip: WinHome managed
hotstrings:
  '::btw::': by the way
```

### Full managed script

```yaml
script_path: "%USERPROFILE%\\Documents\\AutoHotkey\\main.ahk"
settings:
  persistent: true
  detect_hidden_windows: On
hotkeys:
  '#z': 'Run "https://www.google.com"'
  '!Space': 'Send "{Volume_Mute}"'
hotstrings:
  '::sig::': |
    Best regards,
    John Doe
```

## Verification Steps

1. Confirm AutoHotkey v2 is installed and `check_installed` reports success.
2. Apply the configuration.
3. Open the target `.ahk` file and verify it starts with `#Requires AutoHotkey v2.0`.
4. Confirm the managed settings, hotkey, and hotstring blocks were written.
5. Confirm any custom script text outside the managed markers still exists after an update.
6. Run the script with AutoHotkey v2 and test the hotkeys or hotstrings manually.

## Notes / Caveats

- The plugin rewrites only the managed sections and keeps custom content outside those sections.
- `script_path` is required; there is no fallback path.
- The plugin does not accept a separate YAML field for dry-run mode; that comes from the execution
  context.
- Hotstring trigger text is written verbatim, so verify the exact trigger syntax after generation.
