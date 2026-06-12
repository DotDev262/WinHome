# Curl Plugin

## Overview

This plugin manages the configuration file for `curl`, a command line tool for transferring data. It
reads and writes the `_curlrc` file as a series of key=value pairs or standalone flags.

## Prerequisites

- Windows only (if managing Windows paths), but cross-platform logic applies.
- `USERPROFILE` must be set.
- The user must have permission to write to `%USERPROFILE%\_curlrc`.

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                          |
| ---------- | ------ | ------- | ------------------------------------------------------------------------------------ |
| `settings` | object | none    | Merged into `_curlrc`. Each key/value pair is mapped directly to a line in the file. |

The plugin does not validate `curl` configuration options. It simply writes the keys and values you
provide.

### Merge behavior

- Non-object values replace the existing value for the given key.
- New keys are added.
- If a value is a boolean `true`, the key is written as a bare flag (e.g. `silent`).
- If a value is a boolean `false`, the flag is removed.

## Usage Examples

### Minimal update

```yaml
settings:
  proxy: 'http://proxy.example.com:8080'
```

### Multiple flags

```yaml
settings:
  proxy: 'http://proxy.example.com:8080'
  silent: true
  insecure: true
```

## Verification Steps

1. Confirm `curl` is installed.
2. Apply your configuration.
3. Open `%USERPROFILE%\_curlrc`.
4. Verify the expected flags were added or updated.
5. Run `curl` and confirm the settings apply.

## Notes / Caveats

- The plugin never validates whether a key is a real `curl` flag.
- Dry runs are controlled by the execution context, not by a YAML field.
