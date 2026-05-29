# Vim Plugin

## Description

This plugin manages Neovim configuration and plugins on Windows.
It installs Neovim plugins from GitHub and applies settings
to `init.lua` using `config.yaml`.

---

## Configuration

```yaml
plugins:
  - name: vim
    extensions:
      - tpope/vim-fugitive
    settings:
      number: true
```

---

## Supported Settings

| Setting | Type | Description |
|---------|------|-------------|
| extensions | list | Neovim plugins in `user/repo` format (cloned from GitHub) |
| settings | map | Neovim options written to `init.lua` |
| settings.theme | string | Sets the colorscheme |

---

## Example Usage

### Example 1: Install Neovim Plugins

```yaml
plugins:
  - name: vim
    extensions:
      - tpope/vim-fugitive
      - nvim-treesitter/nvim-treesitter
```

### Example 2: Apply Editor Settings

```yaml
plugins:
  - name: vim
    settings:
      number: true
      tabstop: 2
      expandtab: true
```

### Example 3: Full Configuration

```yaml
plugins:
  - name: vim
    extensions:
      - tpope/vim-fugitive
    settings:
      theme: catppuccin
      number: true
      tabstop: 2
```

---

## Verification Steps

- Run WinHome apply
- Open Neovim and verify plugins are loaded
- Check `%LOCALAPPDATA%\nvim\init.lua` for applied settings

---

## Prerequisites

- Neovim must be installed
- `git` must be available in PATH
- Windows OS required