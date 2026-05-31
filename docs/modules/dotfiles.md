# Dotfiles

Links (or copies) files from your WinHome repo into target locations — typically your user profile. WinHome prefers symbolic links and falls back to copying when symlink creation fails.

**YAML Key:** `dotfiles`

**Properties:**

| Property | Type | Description |
| --- | --- | --- |
| `src` | string | Source file path (relative to repo root or absolute) |
| `target` | string | Destination path. Supports `~` and `%USERPROFILE%` |

---

## Basic Usage

Store dotfiles in a `dotfiles/` folder inside your WinHome repository:

```yaml
dotfiles:
  - src: dotfiles/.gitconfig
    target: ~/.gitconfig
  - src: dotfiles/starship.toml
    target: ~/.config/starship.toml
```

Paths are resolved relative to the current working directory when WinHome runs.

---

## Real-World Examples

### Example 1 — Git and shell config

```yaml
dotfiles:
  - src: dotfiles/.gitconfig
    target: ~/.gitconfig
  - src: dotfiles/profile.ps1
    target: ~/Documents/PowerShell/Microsoft.PowerShell_profile.ps1
```

### Example 2 — App config under AppData

```yaml
dotfiles:
  - src: dotfiles/alacritty.toml
    target: "%APPDATA%\\alacritty\\alacritty.toml"
  - src: dotfiles/starship.toml
    target: ~/.config/starship.toml
```

### Example 3 — Neovim / editor layout

```yaml
dotfiles:
  - src: dotfiles/init.lua
    target: ~/AppData/Local/nvim/init.lua
  - src: dotfiles/helix/config.toml
    target: ~/.config/helix/config.toml
```

### Example 4 — Mixed repo-relative and absolute sources

```yaml
dotfiles:
  - src: C:\\Users\\Shared\\templates\\.editorconfig
    target: ~/.editorconfig
  - src: dotfiles/.wslconfig
    target: ~/.wslconfig
```

---

## Edge Cases

- **Existing target file:** WinHome renames the current file to `<target>.bak` before linking.
- **Already linked:** If the target is already a symlink to the same source, the entry is skipped.
- **Symlink permission:** Creating symlinks on Windows may require Developer Mode or an elevated terminal. WinHome falls back to `File.Copy` when symlink creation fails.
- **Source must exist:** Missing source files log an error and skip that entry.
- **Dry run:** `--dry-run` prints the planned link without modifying files.

---

## Troubleshooting

**Issue: `[Dotfile] Error: Source file not found`**

- Run WinHome from the repo root, or use an absolute `src` path.
- Confirm the file is committed and present on disk (not just listed in YAML).

**Issue: Symlink failed, file copied instead**

- Enable **Developer Mode** (Settings → Privacy & security → For developers) or run the terminal as Administrator.
- Check the log for `Symlink failed: ... Falling back to copy.` — edits to the target will no longer sync back to the repo.

**Issue: Target still shows old content**

- If a `.bak` file exists, the previous file was preserved. Delete the target and re-run WinHome if needed.
- For copy fallback, update `src` and re-run WinHome to overwrite the copy.

**Issue: `~` path resolves incorrectly**

- WinHome expands `~` to `%USERPROFILE%`. Use forward or backslashes consistently after `~` (e.g. `~/.gitconfig`).
