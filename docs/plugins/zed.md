# Zed Plugin

## Overview

The Zed plugin manages Zed editor configuration settings in JSONC format.

It can:

- Create or update the Zed configuration file
- Apply custom settings with automatic type coercion
- Merge new settings with existing configuration
- Support explicit config path override

## Prerequisites

- Zed editor installed
- Windows environment with `%APPDATA%` configured
- Permission to write to `%APPDATA%\Zed`

## Configuration Schema

| Field    | Type   | Default | Description                                     |
| -------- | ------ | ------- | ----------------------------------------------- |
| settings | object | none    | Key-value settings written to Zed settings.json |

### Type Coercion

Certain settings are automatically coerced to the correct type:

| Key                   | Type |
| --------------------- | ---- |
| buffer_font_size      | int  |
| font_size             | int  |
| tab_size              | int  |
| vim_mode              | bool |
| relative_line_numbers | bool |
| copilot               | bool |

## Usage Examples

### Basic Configuration

```yaml
extensions:
  zed:
    settings:
      theme: 'One Dark'
      font_size: 14
      vim_mode: true
```

### Full Editor Setup

```yaml
extensions:
  zed:
    settings:
      theme: 'One Dark'
      buffer_font_size: 14
      tab_size: 2
      vim_mode: true
      relative_line_numbers: true
      copilot: false
      features:
        inline_completion: 'copilot'
        edit_prediction: false
```

### Custom Config Path

```yaml
extensions:
  zed:
    settings:
      config_path: "%USERPROFILE%\\.config\\zed\\settings.json"
      theme: 'One Dark'
```

## Verification Steps

1. Apply the configuration.
2. Open `%APPDATA%\Zed\settings.json`.
3. Verify the configured values exist with correct types.
4. Launch Zed.
5. Confirm the settings are reflected in the editor.

## Notes / Caveats

- Existing settings are merged rather than replaced.
- Settings files use JSONC format (supports comments).
- The plugin handles JSONC comments during parsing.
- Integer and boolean values in string form are automatically coerced.
- A corrupt settings file is backed up before being overwritten.
- The `%APPDATA%` environment variable must exist.
- An explicit `config_path` can be provided in settings to override the default path.
