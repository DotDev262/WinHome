# Obsidian Plugin

## Description

This plugin manages Obsidian vaults, community plugins,
and editor settings using `config.yaml`.

---

## Configuration

```yaml
plugins:
  - name: obsidian
    vaults:
      - path: C:\Users\user\Documents\MyVault
        plugins:
          - dataview
        settings:
          theme: obsidian
```

---

## Supported Settings

| Setting | Type | Description |
|---------|------|-------------|
| vaults | list | List of Obsidian vaults to configure |
| vaults[].path | string | Full path to the vault directory |
| vaults[].plugins | list | Community plugin IDs to install |
| vaults[].settings.theme | string | Sets the vault theme |
| vaults[].settings.accentColor | string | Sets the accent color |
| vaults[].settings.baseFontSize | number | Sets the base font size |
| vaults[].settings.showLineNumber | boolean | Show line numbers in editor |
| vaults[].settings.spellcheck | boolean | Enable spellcheck |
| vaults[].settings.vimMode | boolean | Enable Vim keybindings |
| vaults[].settings.readableLineLength | boolean | Enable readable line length |

---

## Example Usage

### Example 1: Install Community Plugins

```yaml
plugins:
  - name: obsidian
    vaults:
      - path: C:\Users\user\Documents\MyVault
        plugins:
          - dataview
          - templater-obsidian
```

### Example 2: Apply Appearance Settings

```yaml
plugins:
  - name: obsidian
    vaults:
      - path: C:\Users\user\Documents\MyVault
        settings:
          theme: obsidian
          baseFontSize: 16
          accentColor: "#7c3aed"
```

### Example 3: Full Configuration

```yaml
plugins:
  - name: obsidian
    vaults:
      - path: C:\Users\user\Documents\MyVault
        plugins:
          - dataview
          - templater-obsidian
        settings:
          theme: obsidian
          baseFontSize: 16
          showLineNumber: true
          vimMode: false
          spellcheck: true
```

---

## Verification Steps

- Run WinHome apply
- Open Obsidian and check vault settings
- Verify community plugins are installed and enabled
- Check `.obsidian/appearance.json` for theme settings

---

## Prerequisites

- Obsidian must be installed
- Vault must already exist at the specified path
- Windows OS recommended