# Mise Plugin

## Overview

This plugin manages the configuration file for `mise`, a dev tools version manager. It preserves
existing TOML formatting as much as possible while deep-merging settings.

## Prerequisites

- Windows only (if managing Windows paths), but cross-platform logic applies.
- `LOCALAPPDATA` must be set.
- The user must have permission to write to `%LOCALAPPDATA%\mise\config.toml`.

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                 |
| ---------- | ------ | ------- | --------------------------------------------------------------------------- |
| `settings` | object | none    | Recursively merged into `config.toml`. Any nested object shape is accepted. |

The plugin does not enforce a mise-specific schema. It simply deep-merges the TOML object tree you
provide.

### Merge behavior

- Objects are merged recursively.
- Non-object values replace the existing value.
- New keys are added.
- If the current file is missing, the plugin starts from an empty object.

## Usage Examples

### Minimal update

```yaml
settings:
  tools:
    node: 'lts'
```

### Nested settings update

```yaml
settings:
  tools:
    node: 'lts'
    python: '3.12'
  settings:
    legacy_version_file: true
    jobs: 4
```

## Verification Steps

1. Confirm `mise` is installed.
2. Apply your configuration.
3. Open `%LOCALAPPDATA%\mise\config.toml`.
4. Verify the expected nested keys were added or updated.
5. Run `mise settings` or similar commands and confirm the setting appears.

## Notes / Caveats

- The plugin never validates whether a key is a real mise option.
- Because merging is recursive, providing a partial object only changes the matching subtree.
- Dry runs are controlled by the execution context, not by a YAML field.
