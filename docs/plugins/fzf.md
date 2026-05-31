# fzf plugin

## Description

The `fzf` plugin manages the fzf fuzzy finder shell configuration file (`_fzfrc` on Windows, `~/.fzfrc` elsewhere). It writes `export KEY="value"` lines so your default fzf options are applied whenever fzf starts in a shell that sources this file.

## Prerequisites

- fzf installed and available as `fzf.exe` or `fzf` on `PATH`
- On Windows, `USERPROFILE` must be set (standard)
- The plugin uses Python stdlib only

## Configuration file location

| Platform | Path |
|----------|------|
| Windows  | `%USERPROFILE%\_fzfrc` |
| Unix     | `~/.fzfrc` |

## Configuration format

Settings live under `extensions.fzf.settings`:

```yaml
extensions:
  fzf:
    settings:
      height: 40%
      border: rounded
```

### Supported settings

| Key | Type | Description |
|-----|------|-------------|
| `FZF_DEFAULT_OPTS` | string | Full default option string passed to fzf |
| `height` | string | Shorthand for `--height` (also merged into `FZF_DEFAULT_OPTS`) |
| `border` | string | Shorthand for `--border` |
| `preview` | string | Shorthand for `--preview` |
| `color` | string | Shorthand for `--color` |
| `bind` | string | Shorthand for `--bind` |
| `layout` | string | Shorthand for `--layout` |
| `FZF_*` | string/bool | Any other `FZF_` environment variable (e.g. `FZF_CTRL_T_OPTS`) |

Shorthand keys (`height`, `border`, etc.) are appended to `FZF_DEFAULT_OPTS`. If you set both `FZF_DEFAULT_OPTS` and shorthands, they are combined.

## Usage examples

### Compact layout with border

```yaml
extensions:
  fzf:
    settings:
      height: 40%
      border: rounded
      layout: reverse
```

### Explicit default options

```yaml
extensions:
  fzf:
    settings:
      FZF_DEFAULT_OPTS: "--height 40% --layout=reverse --border"
```

### Ctrl-T file picker preview

```yaml
extensions:
  fzf:
    settings:
      FZF_CTRL_T_OPTS: "--preview 'bat --color=always {}'"
      FZF_DEFAULT_OPTS: "--height 40% --border"
```

## Notes

- Corrupted `_fzfrc` files are backed up with a `.bak.<uuid>` suffix before replacement.
- Values are written as double-quoted shell exports with proper escaping.
- Supports `dryRun` — logs the target path without writing.
- Config key in WinHome: `extensions.fzf`.

## Verification

After applying:

```powershell
Get-Content $env:USERPROFILE\_fzfrc
fzf --version
# In a shell that sources _fzfrc, run fzf and confirm layout/options match
```
