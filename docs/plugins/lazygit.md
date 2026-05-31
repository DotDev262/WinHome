# lazygit plugin

## Description

The `lazygit` plugin manages lazygit terminal UI configuration on Windows. It deep-merges YAML settings into `%APPDATA%\lazygit\config.yml`, allowing you to declaratively control git UI preferences as part of your WinHome setup.

## Prerequisites

- lazygit must be installed ([github.com/jesseduffield/lazygit](https://github.com/jesseduffield/lazygit))
- lazygit is detected via `lazygit.exe` or `lazygit` on `PATH`
- `APPDATA` must be set (standard on Windows)
- Python with PyYAML available (plugin dependency)

## Configuration file location

| Platform | Path |
|----------|------|
| Windows  | `%APPDATA%\lazygit\config.yml` |

## Configuration format

```yaml
extensions:
  lazygit:
    git:
      pagers:
        - delta
    gui:
      showFileTree: true
```

Plugin args are merged directly into `config.yml` — there is no nested `settings` wrapper. Top-level keys in your WinHome config become top-level lazygit config keys.

## Supported settings

Any valid lazygit YAML key is supported. Common sections include:

| Section | Key | Type | Description |
|---------|-----|------|-------------|
| `gui` | `showFileTree` | boolean | Show file tree panel |
| `gui` | `theme` | object | Color theme overrides |
| `git` | `pagers` | string[] | External pagers (e.g. `delta`) |
| `git` | `overrideGpg` | boolean | Override GPG signing behavior |
| `updates` | `method` | string | Update check method |
| `os` | `editPreset` | string | Editor preset (`nvim`, `vscode`, etc.) |

Refer to lazygit's default config or documentation for the full schema.

## Usage examples

### Enable file tree

```yaml
extensions:
  lazygit:
    gui:
      showFileTree: true
```

### Use delta as pager

```yaml
extensions:
  lazygit:
    git:
      pagers:
        - delta
```

### Editor preset

```yaml
extensions:
  lazygit:
    os:
      editPreset: vscode
```

## Notes

- Objects are deep-merged recursively; lists and scalars replace existing values when they differ.
- Supports `dryRun` mode — logs the target path without writing.
- Config key in WinHome: `extensions.lazygit`.
- Changes take effect the next time lazygit starts.

## Verification

After applying:

```powershell
Get-Content "$env:APPDATA\lazygit\config.yml"
lazygit --version
```

Launch lazygit and confirm the UI reflects your settings (e.g. file tree visibility or pager behavior).
