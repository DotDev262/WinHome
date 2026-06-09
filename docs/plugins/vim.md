# Vim Plugin

## Overview

The Vim plugin manages Neovim plugins and configuration settings.

It can:

- Install Neovim plugins from GitHub repositories
- Remove installed plugins
- Configure Neovim settings through `init.lua`
- Apply themes (colorschemes)
- Manage editor options such as line numbers, tabs, and wrapping

## Prerequisites

- Neovim installed
- Git installed and available in PATH
- Windows environment with `%LOCALAPPDATA%` configured
- Permission to modify Neovim configuration files

## Configuration Schema

| Field | Type | Default | Description |
|---------|---------|---------|---------|
| extensions | array | none | List of GitHub repositories to install as Neovim plugins |
| settings | object | none | Neovim options written to `init.lua` |

### Settings Example

```yaml
settings:
  number: true
  relativenumber: true
  tabstop: 4
```

### Extensions Example

```yaml
extensions:
  - tpope/vim-fugitive
  - nvim-treesitter/nvim-treesitter
```

## Usage Examples

### Basic Editor Configuration

```yaml
settings:
  number: true
  tabstop: 4
  shiftwidth: 4
```

### Install Plugins

```yaml
extensions:
  - tpope/vim-fugitive
  - nvim-lualine/lualine.nvim
```

### Theme Configuration

```yaml
settings:
  theme: desert
  number: true
```

## Verification Steps

1. Apply the configuration.
2. Open Neovim.
3. Verify plugins are installed in the Neovim data directory.
4. Open `%LOCALAPPDATA%\nvim\init.lua`.
5. Confirm settings were written correctly.
6. Verify the configured theme and editor options are active.

## Notes / Caveats

- Plugins are installed directly from GitHub repositories.
- Existing configuration may be overwritten when applying settings.
- The plugin writes configuration to `init.lua`.
- Theme configuration uses Neovim colorschemes.
