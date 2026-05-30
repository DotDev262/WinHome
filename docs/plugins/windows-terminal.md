# Windows Terminal Plugin

## Overview

This plugin deep-merges JSON settings into the active Windows Terminal `settings.json`. It detects stable, preview, dev (Store), or portable (`~/.config/wt/settings.json`) installs automatically.

## Prerequisites

- Windows only
- `LOCALAPPDATA` or `USERPROFILE` must be set
- Windows Terminal installed (Store or portable), or the plugin creates settings at the stable Store path on first apply

## Configuration Schema

Top-level YAML keys under `extensions.windows-terminal` are merged into `settings.json` (same shape as upstream Windows Terminal schema).

| Field | Type | Description |
| --- | --- | --- |
| *(any)* | object / scalar | Recursively merged into the active `settings.json`. Common keys include `defaultProfile`, `profiles`, `schemes`, and `actions`. |

### Merge behavior

- Objects merge recursively; scalars and arrays replace existing values.
- If no settings file exists, the plugin creates one at the stable package path.
- Dry runs are controlled by WinHome execution context, not a YAML field.

## Usage Examples

### Set default profile GUID

```yaml
extensions:
  windows-terminal:
    defaultProfile: "{00000000-0000-0000-0000-000000000001}"
```

### Add a color scheme

```yaml
extensions:
  windows-terminal:
    schemes:
      - name: WinHome Dark
        background: "#1e1e1e"
        foreground: "#cccccc"
        cursorColor: "#ffffff"
```

### Tweak profile defaults (partial merge)

```yaml
extensions:
  windows-terminal:
    profiles:
      defaults:
        font:
          face: Cascadia Mono
          size: 11
```

## Verification Steps

1. Run `where wt` or confirm a Terminal settings file exists under `%LOCALAPPDATA%\Packages\...\LocalState\`.
2. Apply your WinHome config (or dry-run first).
3. Open the detected `settings.json` and confirm merged keys.
4. Launch Windows Terminal and verify profiles, schemes, or defaults changed.

## Notes / Caveats

- The plugin does not validate Windows Terminal schema keys.
- Only one active settings path is updated (first existing path wins).
- Restart Terminal windows to pick up some profile changes.
