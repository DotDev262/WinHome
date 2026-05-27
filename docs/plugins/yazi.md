# Yazi Plugin

## Description

The Yazi plugin manages Yazi configuration files by updating TOML-based configuration settings.

The plugin can:
- Configure Yazi behavior
- Configure keymaps
- Configure themes and flavors
- Automatically create configuration directories
- Merge new settings into existing TOML configuration files
- Detect whether Yazi is installed

## Supported OS

- Windows

## Configuration Directory

```text
%APPDATA%\yazi\config\
```

The plugin manages the following files:

| File | Purpose |
|------|----------|
| yazi.toml | Main Yazi configuration |
| keymap.toml | Keybinding configuration |
| theme.toml | Theme and flavor configuration |

---

## Supported Configuration Sections

### Main Configuration (`yazi.toml`)

| Section |
|----------|
| manager |
| preview |
| opener |
| log |
| plugin |
| input |
| which |
| spotlight |

### Keymap Configuration (`keymap.toml`)

| Section |
|----------|
| keymap |
| prepend_keymap |
| append_keymap |

### Theme Configuration (`theme.toml`)

| Section |
|----------|
| theme |
| flavor |

---

## Configuration

Basic configuration example:

```yaml
plugins:
  - name: yazi
    manager:
      show_hidden: true
```

---

## Usage Examples

### Example 1 — Configure Manager Settings

```yaml
plugins:
  - name: yazi
    manager:
      show_hidden: true
      sort_by: "natural"
```

### Example 2 — Configure Preview Settings

```yaml
plugins:
  - name: yazi
    preview:
      max_width: 1200
      max_height: 900
```

### Example 3 — Configure Keymaps

```yaml
plugins:
  - name: yazi
    keymap:
      manager:
        - on: ["g", "h"]
          run: "cd ~"
```

### Example 4 — Configure Theme

```yaml
plugins:
  - name: yazi
    theme:
      flavor:
        dark: "tokyonight"
```

### Example 5 — Configure Plugins

```yaml
plugins:
  - name: yazi
    plugin:
      prepend_previewers:
        - name: "*.md"
          run: "glow"
```

---

## Verification

Verify Yazi installation:

```bash
yazi --version
```

Verify configuration files exist:

```text
%APPDATA%\yazi\config\
```

Verify TOML configuration:

```bash
cat yazi.toml
```

---

## Notes

- Existing TOML settings are preserved and merged automatically.
- Arrays are merged without duplicating items.
- Unknown configuration keys are ignored.
- Dry-run mode is supported.
- The plugin automatically creates missing configuration directories.
- Python 3.11+ is recommended because `tomllib` is required for TOML parsing.