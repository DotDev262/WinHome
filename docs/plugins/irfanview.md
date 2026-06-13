# IrfanView Plugin

## Overview

This plugin manages IrfanView configuration stored in the user's INI file. Settings are merged into the existing configuration, allowing IrfanView preferences to be managed declaratively through WinHome.

## Prerequisites

* IrfanView installed
* Windows system with `%APPDATA%` available
* Permission to write to `%APPDATA%\IrfanView`

## Configuration File Location

| Platform | Path                               |
| -------- | ---------------------------------- |
| Windows  | `%APPDATA%\IrfanView\i_view64.ini` |

The plugin automatically detects existing `i_view*.ini` files and uses them when available.

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                      |
| ---------- | ------ | ------- | -------------------------------------------------------------------------------- |
| `settings` | object | none    | INI sections and key-value pairs to merge into the IrfanView configuration file. |

### Merge Behavior

* Existing settings are preserved unless overwritten.
* Missing sections are created automatically.
* Boolean values are converted to `1` or `0`.
* New keys are added automatically.

## Usage Examples

### Configure viewing options

```yaml
extensions:
  irfanview:
    settings:
      Viewing:
        FullScreen: 1
        FitToScreen: 1
```

### Configure slideshow settings

```yaml
extensions:
  irfanview:
    settings:
      Slideshow:
        Loop: true
        Random: false
```

## Verification Steps

1. Apply your WinHome configuration.
2. Open `%APPDATA%\IrfanView\i_view64.ini`.
3. Verify the expected sections and keys were added or updated.
4. Launch IrfanView and confirm the settings are reflected in the application.

## Notes / Caveats

* Existing configuration entries are preserved.
* The plugin performs a deep merge of INI sections.
* Corrupted configuration files are backed up automatically before recovery.
* Supports dry-run mode.
* Section and key names are preserved exactly as provided.
