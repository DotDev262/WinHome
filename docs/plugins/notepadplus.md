# Notepad++ Plugin

## Description

This plugin manages Notepad++ configuration on Windows.
It applies editor settings to `config.json` using `config.yaml`.

---

## Configuration

```yaml
plugins:
  - name: notepadplusplus
    settings:
      theme: DarkModeDefault
```

---

## Supported Settings

| Setting | Type | Description |
|---------|------|-------------|
| settings | map | Key-value pairs written to Notepad++ config.json |
| settings.theme | string | Sets the editor theme |
| settings.tabSize | number | Sets the tab size |
| settings.wordWrap | boolean | Enable word wrap |
| settings.lineNumbers | boolean | Show line numbers |

---

## Example Usage

### Example 1: Set Theme

```yaml
plugins:
  - name: notepadplusplus
    settings:
      theme: DarkModeDefault
```

### Example 2: Editor Preferences

```yaml
plugins:
  - name: notepadplusplus
    settings:
      tabSize: 4
      wordWrap: true
      lineNumbers: true
```

### Example 3: Full Configuration

```yaml
plugins:
  - name: notepadplusplus
    settings:
      theme: DarkModeDefault
      tabSize: 4
      wordWrap: true
      lineNumbers: true
```

---

## Verification Steps

- Run WinHome apply
- Open Notepad++
- Check that settings are applied correctly
- Verify `%APPDATA%\Notepad++\config.json` for changes

---

## Prerequisites

- Notepad++ must be installed
- Windows OS required