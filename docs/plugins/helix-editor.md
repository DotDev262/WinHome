# Helix Editor Plugin

## Overview

Manages Helix configuration on Windows by deep-merging TOML into `%APPDATA%\helix\config.toml` and `%APPDATA%\helix\languages.toml`.

## Prerequisites

- Windows (`APPDATA` required)
- Python 3.11+ recommended (uses `tomllib` for reads; without it, existing files may be treated as empty on read)
- Helix optional at apply time

## Configuration Schema

Config key: `extensions.helix-editor`.

| Field | Type | Description |
| --- | --- | --- |
| `config` | object | Merged into `config.toml`. |
| `languages` | object | Merged into `languages.toml`. |
| *(top-level keys)* | object | Keys named `language` or `language-server` go to `languages.toml`; others go to `config.toml`. |

Array-of-tables entries with a `name` field merge by name (e.g. `[[language]]`).

## Usage Examples

### Editor theme and line numbers

```yaml
extensions:
  helix-editor:
    config:
      theme: dark_plus
      editor:
        line-number: true
        mouse: true
```

### Language server entry

```yaml
extensions:
  helix-editor:
    languages:
      language-server:
        - name: rust-analyzer
          command: rust-analyzer
          config:
            checkOnSave: true
```

### Mixed top-level keys (legacy shape)

```yaml
extensions:
  helix-editor:
    theme: gruvbox
    language:
      - name: python
        scope: source.python
```

## Verification Steps

1. Apply config.
2. Inspect `%APPDATA%\helix\config.toml` and `languages.toml`.
3. Run `hx --health` or launch Helix and confirm options/LS entries work.

## Notes / Caveats

- TOML is rewritten via an internal dumper; formatting may differ from hand-edited files.
- Deep merge for nested tables; array-of-tables merge by `name` when present.
- Dry-run logs intended changes without writing files.
