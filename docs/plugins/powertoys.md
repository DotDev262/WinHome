# PowerToys Plugin

## Overview

This plugin manages Microsoft PowerToys configuration files under `%LOCALAPPDATA%\Microsoft\PowerToys`. It can update the main PowerToys settings file and the per-module settings for `FancyZones`, `Awake`, and `PowerRename`.

## Prerequisites

- Windows only.
- Microsoft PowerToys installed.
- `LOCALAPPDATA` must be set.
- The current user must have write access to the PowerToys settings folders under their profile.

## Configuration Schema

The plugin accepts a top-level YAML object with these fields:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `general` | object | none | Updates `%LOCALAPPDATA%\Microsoft\PowerToys\settings.json`. If `raw` and `settings` are both omitted, the whole object is merged into the current general settings file. |
| `modules` | object | none | Optional container for module configs. Keys must be supported module names. |
| `fancyzones` | object | none | Shortcut for configuring `FancyZones`. |
| `awake` | object | none | Shortcut for configuring `Awake`. |
| `powerrename` | object | none | Shortcut for configuring `PowerRename`. |

Supported module names are `fancyzones`, `awake`, and `powerrename`. The plugin maps them to the corresponding PowerToys folders `FancyZones`, `Awake`, and `PowerRename`.

### `general`

`general` may contain:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `raw` | object | none | Merges directly into the top-level PowerToys settings JSON. |
| `settings` | object | none | Also merges into the top-level PowerToys settings JSON. |

If neither `raw` nor `settings` is provided, the plugin merges the remaining keys in `general` directly into the top-level JSON.

### Module config objects

Each supported module config may contain:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `enabled` | boolean | unchanged | Updates the module's `enabled` value. |
| `settings` | object | none | Merges into the module's `properties` object. This is the main way to update nested module settings. |
| `properties` | object | none | Merges into the module's `properties` object. |
| `raw` | object | none | Merges directly into the module JSON object. |

If none of `enabled`, `settings`, `properties`, or `raw` is present, the plugin merges the whole object directly into the module JSON.

### Behavior notes

- Missing settings files are treated as empty JSON objects.
- Existing JSON is preserved and merged recursively.
- If a module file contains invalid JSON, the plugin reports an error instead of overwriting it.
- After a module-only change, the plugin touches the main `settings.json` file so PowerToys notices the update.

## Usage Examples

### Minimal general settings update

```yaml
general:
  settings:
    theme: light
    startup: true
```

### Update one module

```yaml
fancyzones:
  enabled: true
  settings:
    shiftDrag: true
```

### Manage multiple modules at once

```yaml
modules:
  awake:
    enabled: true
  powerrename:
    enabled: false
    raw:
      properties:
        renameFiles: true
```

## Verification Steps

1. Confirm PowerToys is installed and that `%LOCALAPPDATA%\Microsoft\PowerToys` exists.
2. Apply your configuration.
3. Check the relevant file on disk:
   - General settings: `%LOCALAPPDATA%\Microsoft\PowerToys\settings.json`
   - FancyZones: `%LOCALAPPDATA%\Microsoft\PowerToys\FancyZones\settings.json`
   - Awake: `%LOCALAPPDATA%\Microsoft\PowerToys\Awake\settings.json`
   - PowerRename: `%LOCALAPPDATA%\Microsoft\PowerToys\PowerRename\settings.json`
4. Open the file and confirm the expected keys were merged.
5. If you changed a module setting, restart or refresh PowerToys if the UI does not pick up the change immediately.

## Notes / Caveats

- The plugin does not validate the structure of PowerToys keys beyond requiring objects for `raw`, `settings`, and `properties`.
- Unknown module names fail the apply step.
- `check_installed` returns true when the target settings file exists, not when PowerToys itself is currently running.
- Dry runs are controlled by the execution context, not by a YAML field.
