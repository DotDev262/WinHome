# Helix Editor Plugin

## Description

This plugin manages Helix editor configuration on Windows.
It applies settings to `config.toml` and `languages.toml`
using `config.yaml`.

---

## Configuration

```yaml
plugins:
  - name: helix-editor
    config:
      theme: catppuccin_mocha
      editor:
        line-number: relative
```

---

## Supported Settings

| Setting | Type | Description |
|---------|------|-------------|
| config | map | Helix `config.toml` settings |
| config.theme | string | Sets the editor colorscheme |
| config.editor | map | Editor behaviour options |
| languages | map | Helix `languages.toml` settings |
| language | map | Language-specific configuration |
| language-server | map | LSP server configuration |

---

## Example Usage

### Example 1: Set Theme

```yaml
plugins:
  - name: helix-editor
    config:
      theme: catppuccin_mocha
```

### Example 2: Editor Settings

```yaml
plugins:
  - name: helix-editor
    config:
      editor:
        line-number: relative
        mouse: false
        auto-save: true
```

### Example 3: Language Configuration

```yaml
plugins:
  - name: helix-editor
    languages:
      language:
        - name: python
          language-servers: [pylsp]
```

---

## Verification Steps

- Run WinHome apply
- Open Helix editor
- Check `%APPDATA%\helix\config.toml` for applied settings
- Verify language settings in `%APPDATA%\helix\languages.toml`

---

## Prerequisites

- Helix editor must be installed
- `hx` command must be available in PATH
- Windows OS required