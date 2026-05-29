# Windows Terminal Plugin

## Description

This plugin manages Windows Terminal configuration on Windows.
It applies settings directly to `settings.json` using `config.yaml`.

---

## Configuration

```yaml
plugins:
  - name: windows-terminal
    theme: dark
    defaultProfile: PowerShell
```

---

## Supported Settings

| Setting | Type | Description |
|---------|------|-------------|
| theme | string | Sets the terminal color theme |
| defaultProfile | string | Sets the default terminal profile |
| profiles | map | Profile-specific settings |
| actions | list | Custom keybinding actions |

---

## Example Usage

### Example 1: Set Default Profile

```yaml
plugins:
  - name: windows-terminal
    defaultProfile: PowerShell
```

### Example 2: Apply Theme

```yaml
plugins:
  - name: windows-terminal
    theme: dark
    profiles:
      defaults:
        colorScheme: One Half Dark
        fontSize: 12
```

### Example 3: Full Configuration

```yaml
plugins:
  - name: windows-terminal
    theme: dark
    defaultProfile: PowerShell
    profiles:
      defaults:
        colorScheme: One Half Dark
        fontSize: 12
        fontFace: Cascadia Code
```

---

## Verification Steps

- Run WinHome apply
- Open Windows Terminal
- Verify theme and profile settings are applied
- Check `%LOCALAPPDATA%\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json`

---

## Prerequisites

- Windows Terminal must be installed
- Windows OS required