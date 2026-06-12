# Chezmoi Plugin

## Overview

This plugin manages the configuration file for `chezmoi`, a dotfile manager. It preserves existing
YAML formatting as much as possible while deep-merging settings.

## Prerequisites

- Windows only (if managing Windows paths), but cross-platform logic applies.
- `LOCALAPPDATA` must be set.
- The user must have permission to write to `%LOCALAPPDATA%\chezmoi\chezmoi.yaml`.

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                  |
| ---------- | ------ | ------- | ---------------------------------------------------------------------------- |
| `settings` | object | none    | Recursively merged into `chezmoi.yaml`. Any nested object shape is accepted. |

The plugin does not enforce a chezmoi-specific schema. It simply deep-merges the YAML object tree
you provide.

### Merge behavior

- Objects are merged recursively.
- Non-object values replace the existing value.
- New keys are added.
- If the current file is missing, the plugin starts from an empty object.

## Usage Examples

### Minimal update

```yaml
settings:
  data:
    email: 'user@example.com'
```

### Nested settings update

```yaml
settings:
  data:
    email: 'user@example.com'
    name: 'John Doe'
  diff:
    command: 'code'
    args: ['--wait', '--diff']
```

## Verification Steps

1. Confirm `chezmoi` is installed.
2. Apply your configuration.
3. Open `%LOCALAPPDATA%\chezmoi\chezmoi.yaml`.
4. Verify the expected nested keys were added or updated.
5. Run `chezmoi data` or similar commands and confirm the setting appears.

## Notes / Caveats

- The plugin never validates whether a key is a real chezmoi option.
- Because merging is recursive, providing a partial object only changes the matching subtree.
- Dry runs are controlled by the execution context, not by a YAML field.
