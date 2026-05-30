# Obsidian Plugin

## Overview

Configures Obsidian vault settings under `.obsidian/` and can install community plugins from the official registry. Supports multiple vaults in one apply pass.

## Prerequisites

- Windows (vault paths are typically absolute)
- Network access when installing community plugins (GitHub releases + registry JSON)
- Vault directories should exist before applying settings

## Configuration Schema

Config key: `extensions.obsidian` or top-level `obsidian`.

| Field | Type | Description |
| --- | --- | --- |
| `vaults` | array | List of vault entries (see below). |

Each vault entry:

| Field | Type | Description |
| --- | --- | --- |
| `path` | string | Absolute path to the vault root. |
| `settings` | object | Keys mapped to `.obsidian/app.json` or `appearance.json` (see plugin source `SETTING_FILE_MAP`). |
| `plugins` | string[] | Community plugin IDs to install and enable. |

Known `settings` keys include: `accentColor`, `theme`, `cssTheme`, `baseFontSize`, `spellcheck`, `vimMode`, `livePreview`, `defaultViewMode`, and others listed in the plugin.

## Usage Examples

### Vault appearance + spellcheck

```yaml
obsidian:
  vaults:
    - path: C:\Users\me\Documents\Notes
      settings:
        accentColor: "#7c3aed"
        spellcheck: true
        livePreview: true
```

### Install community plugins

```yaml
extensions:
  obsidian:
    vaults:
      - path: D:\Vaults\Work
        settings:
          theme: obsidian
        plugins:
          - calendar
          - dataview
```

### Multiple vaults

```yaml
extensions:
  obsidian:
    vaults:
      - path: C:\Notes\Personal
        settings:
          vimMode: false
      - path: C:\Notes\Work
        settings:
          vimMode: true
          strictLineBreaks: true
```

## Verification Steps

1. Confirm vault paths exist.
2. Apply config; inspect `.obsidian/app.json`, `appearance.json`, and `community-plugins.json`.
3. For plugin installs, verify `.obsidian/plugins/<id>/` contains `main.js` and `manifest.json`.
4. Open Obsidian and confirm settings/plugins are active.

## Notes / Caveats

- Unknown setting keys default to `app.json`.
- Plugin install fetches latest GitHub release assets; offline apply fails for new plugins.
- `check_installed` command expects `vaultPath` + `pluginId` args (engine use).
