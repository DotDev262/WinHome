# Sublime Text Plugin

## Overview

The Sublime Text plugin manages user preferences in Sublime Text by updating the `Preferences.sublime-settings` file.

Settings defined in WinHome are merged into the existing Sublime Text configuration, preserving unrelated settings while updating managed values.

## Prerequisites

- Sublime Text installed
- `subl.exe` or `sublime_text.exe` available in PATH

## Configuration Schema

| Key | Purpose |
|------|----------|
| settings | JSON settings written into `Preferences.sublime-settings` |

Any valid Sublime Text user preference can be placed inside the `settings` block.

## Usage Examples

### Basic Preferences

```yaml
extensions:
  sublime-text:
    settings:
      font_size: 14
      word_wrap: true
      tab_size: 4
```

### Theme And Appearance

```yaml
extensions:
  sublime-text:
    settings:
      theme: "Adaptive.sublime-theme"
      color_scheme: "Packages/Color Scheme - Default/Mariana.sublime-color-scheme"
      highlight_line: true
```

### Editor Behavior

```yaml
extensions:
  sublime-text:
    settings:
      auto_complete: true
      save_on_focus_lost: true
      trim_trailing_white_space_on_save: true
```

## Verification Steps

Open Sublime Text and verify that the configured settings are applied.

You can also inspect:

```text
%APPDATA%\Sublime Text\Packages\User\Preferences.sublime-settings
```

and confirm the configured values are present.

## Notes / Caveats

- Existing settings are preserved unless the same key is managed by WinHome.
- New settings are merged into the current configuration.
- Invalid JSON in the existing settings file may prevent settings from being read correctly.
