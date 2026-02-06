# Plugins & Extensions

WinHome supports a powerful plugin system that allows for first-class configuration of tools like Vim and VSCode. These can be defined in their own top-level sections in `config.yaml`.

## Vim / Neovim

The `vim` section allows you to manage plugins and settings for Neovim (`init.lua`).

### Example
```yaml
vim:
  extensions:
    - "tpope/vim-commentary"
    - "nvim-treesitter/nvim-treesitter"
  settings:
    number: true
    relativenumber: true
    theme: "gruvbox"
```

### Options
- `extensions`: A list of GitHub repositories (`user/repo`) to install.
- `settings`: A dictionary of Lua settings to apply to `init.lua`.
  - `theme`: Translates to `vim.cmd('colorscheme <value>')`.
  - `key: value`: Translates to `vim.opt.<key> = <value>`.

---

## VSCode

The `vscode` section allows you to manage extensions and user settings.

### Example
```yaml
vscode:
  extensions:
    - "dbaeumer.vscode-eslint"
    - "esbenp.prettier-vscode"
  settings:
    "editor.tabSize": 2
    "files.autoSave": "afterDelay"
```

### Options
- `extensions`: A list of VSCode extension IDs to install.
- `settings`: A dictionary of settings to merge into `settings.json`.

---

## Generic Extensions

For other plugins that do not have a dedicated top-level section, use the `extensions` block.

### Example
```yaml
extensions:
  test-echo:
    message: "Hello from Python Plugin!"
```
