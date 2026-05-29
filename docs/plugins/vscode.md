# VSCode Plugin

## Description

This plugin manages Visual Studio Code extensions and settings.
It installs extensions and applies settings.json configurations
using `config.yaml`.

---

## Configuration

```yaml
plugins:
  - name: vscode
    extensions:
      - esbenp.prettier-vscode
    settings:
      editor.fontSize: 14
```

---

## Supported Settings

| Setting | Type | Description |
|---------|------|-------------|
| extensions | list | VSCode extension IDs to install |
| settings | map | VSCode settings.json key-value pairs |
| profiles | map | Named profiles with their own extensions/settings |

---

## Example Usage

### Example 1: Install Extensions

```yaml
plugins:
  - name: vscode
    extensions:
      - esbenp.prettier-vscode
      - ms-python.python
```

### Example 2: Apply Editor Settings

```yaml
plugins:
  - name: vscode
    settings:
      editor.fontSize: 14
      editor.tabSize: 2
      editor.formatOnSave: true
```

### Example 3: Named Profile with Extensions

```yaml
plugins:
  - name: vscode
    profiles:
      Python Dev:
        extensions:
          - ms-python.python
        settings:
          editor.fontSize: 13
```

---

## Verification Steps

- Run WinHome apply
- Open VSCode and verify extensions are installed
- Check settings.json to confirm settings were applied

---

## Prerequisites

- Visual Studio Code must be installed
- `code` command must be available in PATH
- Windows OS recommended