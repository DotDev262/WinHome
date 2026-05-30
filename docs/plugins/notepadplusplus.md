# Notepad++ Plugin

## Overview

Merges JSON settings into `%APPDATA%\Notepad++\config.json`. Useful for declaratively toggling Notepad++ options alongside other WinHome-managed tools.

## Prerequisites

- Windows only
- `APPDATA` must be set
- Notepad++ installed (plugin creates `%APPDATA%\Notepad++` if missing)

## Configuration Schema

Config key: `extensions.notepadplusplus`.

| Field | Type | Description |
| --- | --- | --- |
| `settings` | object | Flat key/value map merged into `config.json`. |

Merge is shallow: each key replaces or adds a top-level property when the value differs.

## Usage Examples

### Enable line numbers and disable single-instance guard

```yaml
extensions:
  notepadplusplus:
    settings:
      lineNumberMargin: true
      allowOnlyOneInstance: false
```

### Dark mode preference (example keys)

```yaml
extensions:
  notepadplusplus:
    settings:
      darkMode: true
      noCrapMenu: true
```

### Minimal touch

```yaml
extensions:
  notepadplusplus:
    settings:
      rememberLastSession: true
```

## Verification Steps

1. Confirm `%APPDATA%\Notepad++` exists (or will be created).
2. Apply config.
3. Open `%APPDATA%\Notepad++\config.json` and verify keys.
4. Launch Notepad++ and confirm behavior matches (may require restart).

## Notes / Caveats

- The plugin does not validate Notepad++ config keys; refer to Notepad++ documentation or an existing `config.json` export.
- Only top-level keys are merged (no nested object merge).
- Dry-run logs the would-be update without writing.
