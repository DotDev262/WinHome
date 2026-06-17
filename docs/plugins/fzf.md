# FZF Plugin

## Overview

The fzf plugin manages configuration for `fzf` via the `.fzfrc` or `_fzfrc` file by exporting environment variables like `FZF_DEFAULT_OPTS`.

## Prerequisites

- fzf installed
- Available in PATH

## Configuration Schema

| File | Purpose |
| ---- | ------- |
| .fzfrc / _fzfrc | fzf environment variables |

Supported direct option settings: `height`, `border`, `preview`, `color`, `bind`, `layout`.
Also supports setting any environment variable starting with `FZF_`.

## Usage Examples

### fzf config

```yaml
extensions:
  fzf:
    settings:
      layout: "reverse"
      border: "rounded"
      FZF_CTRL_T_COMMAND: "rg --files --hidden"
```

## Verification Steps

```bash
fzf --version
```

## Notes / Caveats

- Values containing spaces are safely shell-quoted before being written.
- Automatically handles Windows (`_fzfrc`) versus Linux/macOS (`.fzfrc`) paths.
