# Bat Plugin

## Overview

This plugin manages the configuration file for `bat`, a syntax-highlighting pager. It reads and
writes the `config` file as a series of key=value pairs or standalone flags.

## Prerequisites

- Windows only (if managing Windows paths), but cross-platform logic applies.
- `APPDATA` must be set.
- The user must have permission to write to `%APPDATA%\bat\config`.

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                                                 |
| ---------- | ------ | ------- | ----------------------------------------------------------------------------------------------------------- |
| `settings` | object | none    | Merged into `bat/config`. Each key/value pair is mapped directly to a command-line flag format in the file. |

The plugin does not validate `bat` configuration options. It simply writes the keys and values you
provide.

### Merge behavior

- Non-object values replace the existing value for the given key.
- New keys are added.
- If a value is a boolean `true`, the key is written as a bare flag (e.g. `--italic-text`).
- If a value is a boolean `false`, the flag is removed.

## Usage Examples

### Minimal update

```yaml
settings:
  '--theme': 'TwoDark'
```

### Multiple flags

```yaml
settings:
  '--theme': 'TwoDark'
  '--style': 'numbers,changes,header'
  '--italic-text': true
```

## Verification Steps

1. Confirm `bat` is installed.
2. Apply your configuration.
3. Open `%APPDATA%\bat\config`.
4. Verify the expected flags were added or updated.
5. Run `bat` on a file and confirm the visual changes apply.

## Notes / Caveats

- The plugin never validates whether a key is a real `bat` flag.
- Dry runs are controlled by the execution context, not by a YAML field.
