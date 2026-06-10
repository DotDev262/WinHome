# FZF Plugin

## Overview

The FZF plugin manages environment variables and default flags for the `fzf` fuzzy finder.

## Prerequisites

- FZF installed (`fzf.exe` or `fzf` in PATH)

## Configuration Schema

Configuration is written to:
- Windows: `%USERPROFILE%\_fzfrc`
- Linux/macOS: `~/.fzfrc`

Supported settings keys:

| Key | Description |
|-----|-------------|
| height | Sets fzf window height (appended to `FZF_DEFAULT_OPTS`) |
| border | Sets fzf border style (appended to `FZF_DEFAULT_OPTS`) |
| preview | Sets fzf preview command (appended to `FZF_DEFAULT_OPTS`) |
| color | Sets fzf color scheme (appended to `FZF_DEFAULT_OPTS`) |
| bind | Sets fzf key bindings (appended to `FZF_DEFAULT_OPTS`) |
| layout | Sets fzf layout style (appended to `FZF_DEFAULT_OPTS`) |
| FZF_DEFAULT_OPTS | Sets explicit default options string |
| Any key starting with `FZF_` | Written directly as `export KEY="value"` in the config file |

## Usage Examples

```yaml
plugins:
  - name: fzf
    settings:
      height: "40%"
      layout: "reverse"
      border: "rounded"
      FZF_DEFAULT_COMMAND: "fd --type f"
```

## Verification Steps

To verify fzf installation:
```bash
fzf --version
```
To inspect the configured environment settings, examine the fzf configuration file:
```bash
cat ~/.fzfrc
```

## Notes / Caveats

- Sourcing the `_fzfrc` or `.fzfrc` file in your shell startup script (e.g. PowerShell profile or `.bashrc`) is required for the options to take effect.
