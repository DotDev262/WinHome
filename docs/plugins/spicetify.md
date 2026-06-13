# Spicetify Plugin

## Overview

This plugin manages the Spicetify configuration file located in the user's profile directory. Settings are merged into the existing `config.ini`, allowing Spicetify customization to be managed declaratively through WinHome.

## Prerequisites

* Spicetify installed
* Spotify installed
* Windows system with `%USERPROFILE%` available
* Permission to write to `%USERPROFILE%\.spicetify`

## Configuration File Location

| Platform | Path                                  |
| -------- | ------------------------------------- |
| Windows  | `%USERPROFILE%\.spicetify\config.ini` |

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                      |
| ---------- | ------ | ------- | -------------------------------------------------------------------------------- |
| `settings` | object | none    | INI sections and key-value pairs to merge into the Spicetify configuration file. |

### Merge Behavior

* Existing settings are preserved unless overwritten.
* Missing sections are created automatically.
* Boolean values are converted to `1` or `0`.
* Lists are stored as comma-separated values.
* New keys are added automatically.

## Usage Examples

### Configure theme settings

```yaml
extensions:
  spicetify:
    settings:
      Setting:
        current_theme: Marketplace
        inject_css: true
        inject_theme_js: true
```

### Configure extensions and custom apps

```yaml
extensions:
  spicetify:
    settings:
      AdditionalOptions:
        extensions:
          - adblock.js
          - shuffle.js
        custom_apps:
          - marketplace
```

## Verification Steps

1. Apply your WinHome configuration.
2. Open `%USERPROFILE%\.spicetify\config.ini`.
3. Verify the expected sections and keys were added or updated.
4. Run Spicetify and confirm the settings are reflected in Spotify.

## Notes / Caveats

* Existing configuration entries are preserved.
* The plugin performs a section-based merge.
* Corrupted configuration files are automatically backed up before recovery.
* Supports dry-run mode.
* Lists are written as comma-separated values.
