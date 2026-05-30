# Vim / Neovim Plugin

## Overview

Generates `%LOCALAPPDATA%\nvim\init.lua` from `settings` and installs Git-based Neovim plugins under `%LOCALAPPDATA%\nvim-data\site\pack\winhome\start`. Despite the name, this plugin targets **Neovim** on Windows.

## Prerequisites

- Windows
- `git` on `PATH` for plugin clones
- Neovim optional at apply time; config is written regardless

## Configuration Schema

Config key: `extensions.vim` or top-level `vim`.

| Field | Type | Description |
| --- | --- | --- |
| `settings` | object | Maps to `vim.opt.*` or `colorscheme` in generated `init.lua`. |
| `extensions` | string[] | GitHub `user/repo` IDs cloned into the WinHome pack path. |

Supported `settings` value types:

- `theme` (string) → `vim.cmd('colorscheme …')`
- boolean / bool-like strings → `vim.opt.<key> = true|false`
- string → quoted `vim.opt.<key>`
- integer → numeric `vim.opt.<key>`

Also supports `apps:` with `manager: vim` and `id: user/repo`.

## Usage Examples

### Basic editor options

```yaml
vim:
  settings:
    number: true
    relativenumber: true
    tabstop: 4
    theme: habamax
```

### Plugins + settings

```yaml
extensions:
  vim:
    settings:
      shiftwidth: 2
    extensions:
      - tpope/vim-fugitive
      - tpope/vim-surround
```

### Single plugin via apps

```yaml
apps:
  - id: tpope/vim-fugitive
    manager: vim
```

## Verification Steps

1. Apply config.
2. Open `%LOCALAPPDATA%\nvim\init.lua` and confirm generated lines.
3. Check `%LOCALAPPDATA%\nvim-data\site\pack\winhome\start\<repo>` for cloned plugins.
4. Launch `nvim` and run `:checkhealth` or verify options/plugins load.

## Notes / Caveats

- `init.lua` is **regenerated** from settings each apply (not merged with hand-edited Lua).
- Plugin IDs must be valid `github.com/user/repo` paths.
- Re-running apply is idempotent for already-cloned plugins.
