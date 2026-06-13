# Dotfiles

Manages configuration files (dotfiles) for your Windows development environment.

**YAML Key:** `dotfiles`

**Properties:**

- `source` : Path to source dotfile.
- `destination` : Where to place the file.
- `overwrite` : `true` or `false`.

---

## Basic Usage

```yaml
dotfiles:
  - source: "dotfiles\\.gitconfig"
    destination: "~\\.gitconfig"
    overwrite: true
```

---

## Real-World Examples

### Example 1 — Git Config

```yaml
dotfiles:
  - source: "dotfiles\\.gitconfig"
    destination: "~\\.gitconfig"
    overwrite: true
```

### Example 2 — Multiple Configs

```yaml
dotfiles:
  - source: "dotfiles\\.gitconfig"
    destination: "~\\.gitconfig"
    overwrite: true
  - source: "dotfiles\\.bashrc"
    destination: "~\\.bashrc"
    overwrite: false
```

### Example 3 — VSCode Settings

```yaml
dotfiles:
  - source: "dotfiles\\settings.json"
    destination: "%APPDATA%\\Code\\User\\settings.json"
    overwrite: true
```

### Example 4 — Terminal Config

```yaml
dotfiles:
  - source: "dotfiles\\terminal.json"
    destination: "%LOCALAPPDATA%\\Packages\\terminal\\settings.json"
    overwrite: true
```

---

## Troubleshooting

**Issue: File not copied**

- Check if source path is correct
- Make sure source file exists
- Run WinHome as Administrator

**Issue: Overwrite not working**

- Set overwrite to true in config
- Check file permissions
- Make sure destination path is correct

**Issue: Wrong destination**

- Use full path for destination
- Check Windows path format
- Use %APPDATA% for app configs
