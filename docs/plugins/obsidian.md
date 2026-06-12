# Obsidian Plugin

## Overview

The Obsidian plugin manages Obsidian vault settings and community plugins.

It can:

- Configure Obsidian vault settings
- Install community plugins
- Uninstall community plugins
- Manage multiple vaults
- Update appearance and editor preferences
- Enable or disable community plugins

## Prerequisites

- Obsidian installed
- Existing Obsidian vault(s)
- Permission to modify files inside the vault's `.obsidian` directory

## Configuration Schema

| Field             | Type   | Default | Description                       |
| ----------------- | ------ | ------- | --------------------------------- |
| vaults            | array  | none    | List of Obsidian vaults to manage |
| vaults[].path     | string | none    | Path to the vault                 |
| vaults[].settings | object | none    | Vault settings                    |
| vaults[].plugins  | array  | none    | Community plugins to install      |

### Settings Example

```yaml
vaults:
  - path: 'D:/Notes'
    settings:
      theme: dark
      vimMode: true
      spellcheck: true
```

### Plugin Installation Example

```yaml
vaults:
  - path: 'D:/Notes'
    plugins:
      - calendar
      - dataview
```

## Usage Examples

### Configure Vault Settings

```yaml
vaults:
  - path: 'D:/Notes'
    settings:
      theme: dark
      spellcheck: true
      vimMode: true
```

### Install Community Plugins

```yaml
vaults:
  - path: 'D:/Notes'
    plugins:
      - calendar
      - dataview
```

### Configure Multiple Vaults

```yaml
vaults:
  - path: 'D:/Work'
    settings:
      spellcheck: true

  - path: 'D:/Personal'
    settings:
      vimMode: true
      theme: dark
```

## Verification Steps

1. Apply the configuration.
2. Open Obsidian.
3. Open the configured vault.
4. Verify settings have been updated.
5. Open Community Plugins and verify plugins are installed.
6. Confirm plugin functionality inside the vault.

## Notes / Caveats

- Settings are stored inside the vault's `.obsidian` directory.
- Existing settings are merged with new values.
- Community plugins are downloaded from the official Obsidian plugin registry.
- Plugin installation requires internet connectivity.
- Vault paths must already exist.
